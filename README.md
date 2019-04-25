# CollectorSDK
A .Net SDK for Ivanti Connectors that allow the loading, transforming and publishing data.

# Introduction
The purpose of this document is to describe on going changes to the Collector SDK version 2 and beyond.


# Piped Converters
Any Extract Load Transform (ETL) system allows you to pipe data from one transformation to another.  This is complex and was not required for version 1.x.  Piped Converters is the first step towards this concept.  Piped converters allow you to:

Associate multiple converters with a single data point key (now called a Primary Key).
Targeted converters to have their own left side mapping at run time.
Data from one converter is piped to other configured converters.  The piping of the data has two concepts.
Data is saved (the data returned from the mapping operation) when the last converter is invoked.
Data is saved at each conversion point.  Each converter gets the original data point plus all other previously converted data points. As each converter is invoked, any converted data is saved (i.e. this data will be eventually be published).

# Configuration Changes
The following changes have been made to support piped converters.

Collector Version - There is now a version within the Collector configuration as a double.  Default is 1.0 (backward support).  Version 2.0 + indicates piped converter support.
Mappers.PipedConverters - These are all the configured converters, this overrides Mapped.Converters.
Id - The id of the converter. (required)
Type - Class type of the converter. (required)
Mappers.SourceTargetMappings - Defined the mappings between the inbound data and the piped converters.
PrimaryKey - The data point key.
Properties - A dictionary of name/value pairs.
TargetConverters - A list of converters to invoke (in order) for this data point.
Id - Id of the converter.
CombineInputOutput- Set to true if both the input to a converter and the output should be combined
Nested - Set to true if the output should be run through all the mappings again.  This allows you to nest conversions of a hierarchy. 
LeftSideMap - Rules when converting the primary key to another key.
 

# Example Configuration
In the example below the Mapper is configured for two Converters: CombineDataTimeConverter and DateTimeUtcConverter.  Note the lack of the LeftSideMap.  This is now injected into the Converter at run time, not configuration time.  It is now defined in the SourceTargetMappings.  In the SourceTargetMappings example we are not saving the converted data until the end (PipedData = true).  The PrimaryKey is the "Date" field within the row of data signaled by the Reader.  That data point is first fed into the Converter that expects a "Time" field in the data row and combines those into a date time.  The combined date time is then fed into the UTC Converter.  The output that gets saved (and eventually published) is the combined date time converted to UTC.  See these tests to get an idea of how to use the piped converters.

...

"Mappers": [
   {
      "Id": "50",
      "TransformerId": "40",
      "Type": "Collector.SDK.Mappers.DefaultMapper,Collector.SDK",
      "PipedConverters": [
         {
            "Id": "1",
            "Type": "Collector.SDK.Converters.CombineDateTimeConverter,Collector.SDK",
            "Properties": {}
         },
         {
            "Id": "2",
            "Type": "Collector.SDK.Converters.DateTimeUtcConverter,Collector.SDK",
            "Properties": {}
         }
      ],
      "SourceTargetMappings": [
         {
            "PrimaryKey": "Date",
            "TargetConverters": [
               {
                  "Id": "1",
                  "CombineInputOutput": "false",

                  "NestOutput": "true",

                  "LeftSideMap": { "Date": [ "DateTime" ] }
               },
               {
                  "Id": "2",

                  "CombineInputOutput": "true",

                  "NestOutput": "false",

                  "LeftSideMap": { "DateTime": [ "DateTimeUTC" ]
               },
               Properties:{}

            ]

         }
      ]
   }
]

 

# Mapping/Piping Concepts
A couple of new Mapping concepts have been introduced: Piping of data and nested conversions. 

The piping of data is currently based on two rules:

The mapper does not attempt to save the input, it only cares about the output.  In this case it is up to the converters to decide what to output.
The mapper will combine the input with the output.  This is for the case where you want to create an additional property. For example converting a local date time to UTC would result in the local date time as well as the local date time in UTC (example configuration above).
The nesting of converters allows you to nest the output.  This is an implicit piping versus explicit (contained within one source target map).  If enable on a converter, the mapper will take the output and run it through all source target mappings, not just the containing map.

See these tests to get a better idea of how these new mappers can be used.

# About the Tests

Test: PipedConverters_PipedArray_Success
In this test a data point contains an array of objects (software items).  This is testing the PipedArrayConverter and its ability to do a nested conversion an array of objects.  The result is a dictionary of dictionaries based on an id. The id is determined in the SourceTargetConverter property "ArrayKey".  The property "PrefixId" means to prefix the id with the "ArrayKey", i.e. "Id_XXX" where XXX is the Id field of the software.. The PipedArrayConverter expects the data point to be a list of objects.  It then iterates through the list and uses reflection to access the getters for each field in that object.  For each field it attempts to look up a converter by name.  The name must be contained in the left side mapping of the converter.  If a converter is found it converts the fields values and inserts the result into the output. It can can do all this because the SourceTargetMapping and the Converters in that mapping are now accessible by the Converter. Note: In a real implementation this is dependent on the mapper, the mapper must create the converter list and configure each of them properly at run time, see the AbstractMapper.ConvertDataPoint() method.

Test: PipedConverters_ConvertObjectArray_Success
This tests the same concepts as above except that it uses a configuration file instead of programmatic configuration.  This not only tests the converter but the mapper as well.

Test: PipedConverters_ConvertDelimitedDataPoint_FileConfig_Success
This tests nesting and piping of data.  A converter creates multiple data points from a single data point that is a delimited array of values.  The DelimitedConverter takes as a property an array of delimiters.  It uses the array to split the data point's string value into an array of values.  The values key is computed by the LeftSideMap which contains a column index as the key.  In this example the configuration for the converter looks like:

{

"PrimaryKey": "Items",
"Properties": {
   "ArrayDelimeters": ","
},
"TargetConverters": [
   {
      "Id": "3",
      "NestOutput": "true",
      "CombineInputOutput": "false",
      "LeftSideMap": {
            "0": [ "Name" ],
            "1": [ "Time" ],
            "2": [ "Date" ],
            "3": [ "SerialNumber" ],
         }
      }
] }

This means the first four values of a delimited string of "Some Software Title,01/17/2018,01:23:03.000,00000-789-1234567890" will convert to { "Name":"Some Software Title", "Time":"01:23:03.000", Date": "01/17/2018", "SerialNumber": "00000-789-1234567890" }.  Since this is a converter that has its NestOutput set to true, the mapper then runs the output of the DelimitedConverter against the rest of the configured converters.  Running all of the output data points against any other configured converters.  In this example there are the following converters:
{
   "PrimaryKey": "Date",
   "TargetConverters": [
   {
      "Id": "1",
      "NestOutput": "true",
      "CombineInputOutput": "false",
      "LeftSideMap": {
         "Date": [ "DateTime" ]
      }
   } ]

},
{
   "PrimaryKey": "DateTime",
   "TargetConverters": [
   {
      "Id": "2",
      "NestOutput": "false",
      "CombineInputOutput": "true",
      "LeftSideMap": {
         "DateTime": [ "DateTimeUTC" ]
      }
   } ]
},
{
   "PrimaryKey": "Name",
   "TargetConverters": [

   {
         "Id": "4",
         "NestOutput": "false",
         "CombineInputOutput": "false",
         "LeftSideMap": {
            "Name": [ "Title" ]
         }
   } ]
},
{
   "PrimaryKey": "SerialNumber",
   "TargetConverters": [
   {
      "Id": "4",
      "NestOutput": "false",
      "CombineInputOutput": "false",
      "LeftSideMap": {
      "SerialNumber": [ "Id" ]
      }
   } ]
}

In this example "Date" pipes into CombineDateTimeConverter, which pipes into DateTimeUtcConverter.  The output of this conversion is 2 data points: "DateTime" and "DateTimeUtc".  This is because the CombineInputOutput is set to true in the DateTimeUtcConverter, which means the input "DateTime" is combined with the output "DateTimeUtc"  The "Name" and "SerialNumber" are both piped to the NoOpConverter. These are all returned as a dictionary that is then converted into a MockSoftware object (the mapper DataType is set to "Collector.SDK.Tests.Mocks.MockTransformer,Collector.SDK.Tests").
