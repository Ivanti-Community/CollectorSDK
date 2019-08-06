using System.Collections.Generic;
using Collector.SDK.Collectors;
using Collector.SDK.Configuration;
using Collector.SDK.Converters;
using Collector.SDK.DataModel;
using Collector.SDK.Logging;
using Collector.SDK.Mappers;

namespace Collector.SDK.ActiveDirectory.Mappers
{
    public class PipedMapper : IMapper
    {
        private readonly ILogger _logger;
        private readonly ICollector _collector;

        private string _id;
        private string _dataType;
        private string _transformerId;
        private Dictionary<string, IConverter> _converters;
        private List<SourceTargetMapping> _mappings;

        public string Id => _id;
        public string DataType => _dataType;
        public string TransformerId => _transformerId;
        public Dictionary<string, IConverter> Converters => _converters;
        public List<SourceTargetMapping> Mappings => _mappings;

        public PipedMapper(ICollector collector, ILogger logger)
        {
            _collector = collector;
            _logger = logger;
        }

        public void Configure(string Id, string dataType, string transformerId, Dictionary<string, IConverter> converters)
        {
            _id = Id;
            _dataType = dataType;
            _transformerId = transformerId;
            _converters = converters;
        }

        public void Configure(MapperConfiguration config, Dictionary<string, IConverter> converters)
        {
            _id = config.Id;
            _dataType = config.DataType;
            _transformerId = config.TransformerId;
            _mappings = config.SourceTargetMappings;
            _converters = converters;
        }

        /// <summary>
        /// Pipe the data based on two rules.  
        ///     1. pipeData=true, piped in data is lost. 
        ///     2. pipeData=false, piped in data is combined with the output.
        /// </summary>
        /// <param name="combineInputOutput">Which pipe rule to use.</param>
        /// <param name="convertedDataPointsIn">The input</param>
        /// <param name="convertedDataPointsOut">The output</param>
        /// <returns>The output based on the piping rules.</returns>
        public Dictionary<string, object> CombineData(bool combineInputOutput,
            Dictionary<string, object> convertedDataPointsIn, Dictionary<string, object> convertedDataPointsOut)
        {
            var convertedDataPoints = new Dictionary<string, object>();
            if (convertedDataPointsOut != null && !combineInputOutput)
            {
                // Add only the output, input is thrown away.
                convertedDataPoints = convertedDataPointsOut;
            }
            else
            {
                // Add both input and output
                if (convertedDataPointsIn != null)
                {
                    foreach (var p in convertedDataPointsIn)
                    {
                        if (!convertedDataPoints.ContainsKey(p.Key))
                        {
                            convertedDataPoints.Add(p.Key, p.Value);
                        }
                    }
                }
                if (convertedDataPointsOut != null)
                {
                    foreach (var p in convertedDataPointsOut)
                    {
                        if (!convertedDataPoints.ContainsKey(p.Key))
                        {
                            convertedDataPoints.Add(p.Key, p.Value);
                        }
                    }
                }
            }
            return convertedDataPoints;
        }

        /// <summary>
        /// Convert a single data point.
        /// </summary>
        /// <param name="dataPoint">The data point to convert</param>
        /// <param name="dataRow">The associated row data points</param>
        /// <returns>A dictionary of converted data.</returns>
        public Dictionary<string, object> ConvertDataPoint(SourceTargetMapping mapping, KeyValuePair<string, object> dataPoint, IEntityCollection dataRowIn)
        {
            Dictionary<string, object> result = null;
            if (mapping != null)
            {
                var found = false;
                foreach (var targetConverter in mapping.TargetConverters)
                {
                    if (targetConverter.LeftSideMap != null && targetConverter.LeftSideMap.ContainsKey(dataPoint.Key))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return result;
                }
                // make our local copy of the data row, we may need to manipulate it for nested conversions.
                var dataRow = new EntityCollection();
                foreach (var e in dataRowIn.Entities)
                {
                    dataRow.Entities.Add(e.Key, e.Value);
                }
                var converters = new List<IConverter>();
                // first configure all of the trageted converters, 
                // this in case a piped converter wants to run the other converters internally
                foreach (var targetConverter in mapping.TargetConverters)
                {
                    if (Converters.ContainsKey(targetConverter.Id))
                    {
                        var config = new ConverterConfiguration()
                        {
                            Id = targetConverter.Id,
                            CombineInputOutput = targetConverter.CombineInputOutput,
                            NestOutput = targetConverter.NestOutput,
                            LeftSideMap = targetConverter.LeftSideMap,
                            Properties = (targetConverter.Properties.Count > 0) ? targetConverter.Properties : mapping.Properties,
                            PipedConverters = converters,
                            Mapping = mapping
                        };
                        IConverter converter = CollectorFactory.CloneConverter(Converters[targetConverter.Id]);
                        if (converter != null)
                        {
                            converter.Configure(config);
                            converters.Add(converter);
                        }
                    }
                }
                Dictionary<string, object> convertedDataPoints = null;
                foreach (var targetConverter in mapping.TargetConverters)
                {
                    IConverter converter = null;
                    foreach (var c in converters)
                    {
                        if (c.Id.Equals(targetConverter.Id))
                        {
                            converter = c;
                            break;
                        }
                    }
                    if (converter != null)
                    {
                        Dictionary<string, object> convertedDataPointsOut = null;
                        if (convertedDataPoints != null)
                        {
                            convertedDataPointsOut = converter.Convert(convertedDataPoints, dataRow);
                        }
                        else
                        {
                            convertedDataPointsOut = converter.Convert(dataPoint, dataRow);
                            // If we need to combine input and output, and this is our first converter.
                            // Otherwise the data point will be lost.
                            if (targetConverter.CombineInputOutput)
                            {
                                convertedDataPoints = new Dictionary<string, object>();
                                convertedDataPoints.Add(dataPoint.Key, dataPoint.Value);
                            }
                        }
                        convertedDataPoints = CombineData(targetConverter.CombineInputOutput, convertedDataPoints, convertedDataPointsOut);
                    }
                    result = convertedDataPoints;
                    if (!targetConverter.Pipe)
                    {
                        break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Find the source target mapping based on a primary key.
        /// </summary>
        /// <param name="entities">The data points to iterate over.</param>
        /// <param name="ranMappings">The mapping that have already been run</param>
        /// <returns>The mapping if found, otherwise null</returns>
        public SourceTargetMapping FindMapping(Dictionary<string, object> entities, Dictionary<string, SourceTargetMapping> ranMappings)
        {
            foreach (var dataPoint in entities)
            {
                foreach (var mapping in Mappings)
                {
                    if (!ranMappings.ContainsKey(mapping.PrimaryKey))
                    {
                        var primaryKey = mapping.PrimaryKey;

                        if (primaryKey.Contains("*"))
                        {
                            primaryKey = primaryKey.Substring(0, primaryKey.IndexOf("*"));
                            if (dataPoint.Key.StartsWith(primaryKey))
                            {
                                return mapping;
                            }
                        }
                        else if (primaryKey.Equals(dataPoint.Key))
                        {
                            return mapping;
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Convert an entire row of data.
        /// </summary>
        /// <param name="dataRow">The row to convert</param>
        /// <returns>The converted row.</returns>
        public Dictionary<string, object> ConvertDataRow(SourceTargetMapping mapping, IEntityCollection dataRow)
        {
            var mappedRow = new Dictionary<string, object>();
            // Convert the data points...
            foreach (var dataPoint in dataRow.Entities)
            {
                var convertedDataPoints = ConvertDataPoint(mapping, dataPoint, dataRow);
                if (convertedDataPoints != null)
                {
                    foreach (var convertedDataPoint in convertedDataPoints)
                    {
                        // Last conversion wins
                        if (mappedRow.ContainsKey(convertedDataPoint.Key))
                        {
                            mappedRow[convertedDataPoint.Key] = convertedDataPoint.Value;
                        }
                        else
                        {
                            mappedRow.Add(convertedDataPoint.Key, convertedDataPoint.Value);
                        }
                    }
                }
            }
            return mappedRow;
        }
        /// <summary>
        /// Version 2.x mapping
        /// </summary>
        /// <param name="data">The data to convert</param>
        /// <returns>The converted data</returns>
        public List<object> Map(List<IEntityCollection> data)
        {
            var convertedDataRows = new List<object>();
            // for each row of data...
            foreach (var dataRow in data)
            {
                // The mappings already executed.
                var ranMappings = new Dictionary<string, SourceTargetMapping>();
                // The converted row.
                var mappedRow = new EntityCollection();
                while (true)
                {
                    // Find the next mapping to execute.
                    var mapping = FindMapping(dataRow.Entities, ranMappings);
                    // If now more mappings were found, then we are done.
                    if (mapping == null)
                    {
                        break;
                    }
                    // Run this mapping only once per row
                    ranMappings.Add(mapping.PrimaryKey, mapping);

                    // Convert the data points...
                    mappedRow.Entities = ConvertDataRow(mapping, dataRow);
                    
                    // Loose any data that was not converted
                    dataRow.Entities = mappedRow.Entities;
                }
                convertedDataRows.Add(mappedRow);
            }
            return convertedDataRows;
        }
    }
}
