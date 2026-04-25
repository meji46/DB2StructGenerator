using DBDefsLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB2StructGenerator.StructGenerators
{
    public class CsStructGenerator : StructGeneratorBase
    {
        public CsStructGenerator(ConcurrentDictionary<string /*DB2Name*/, Tuple<Structs.DBDefinition, Structs.VersionDefinitions>> dbddefinitions, int expectedBuildNumber) :
            base(dbddefinitions, expectedBuildNumber) { }

        public override void GenerateStructs()
        {
            Directory.CreateDirectory("CsStructs");

            Parallel.ForEach(definitions, pair =>
            {
                using StreamWriter writer = new($"CsStructs\\{pair.Key.Replace("_", "")}Entry.cs");
                writer.WriteLine("using DBCD.IO.Attributes;");
                writer.WriteLine();
                writer.WriteLine($"namespace WowPacketParser.DBC.Structures.{GetExpansionNameForBuild(pair.Value.Item2.builds)}");
                writer.WriteLine("{");
                writer.WriteLine($"{tabSpaces}[DBFile(\"{pair.Key}\")]");
                writer.WriteLine($"{tabSpaces}public sealed class {pair.Key.Replace("_", "")}Entry");
                writer.WriteLine(tabSpaces + "{");

                FieldValue[] fields = GenerateFields(pair.Value.Item1, pair.Value.Item2);
                ReadOnlySpan<FieldValue> span = fields.AsSpan();
                foreach (FieldValue field in span)
                {
                    if (field.ArraySize > 0)
                    {
                        writer.WriteLine($"{tabSpaces}{tabSpaces}[Cardinality({field.ArraySize})]");
                        writer.WriteLine($"{tabSpaces}{tabSpaces}public {field.FieldType}[] {field.FieldName} = new {field.FieldType}[{field.ArraySize}];");
                        continue;
                    }

                    if (field.Index)
                        writer.WriteLine($"{tabSpaces}{tabSpaces}[Index({field.NoInline.ToString().ToLower()})]");

                    if (field.IsRelation && field.NoInline)
                        writer.WriteLine($"{tabSpaces}{tabSpaces}[Relation(typeof(u{field.FieldType}), true)]");

                    writer.WriteLine($"{tabSpaces}{tabSpaces}public {field.FieldType} {field.FieldName};");
                }

                writer.WriteLine(tabSpaces + "}");
                writer.WriteLine("}");
            });
        }

        public override FieldValue GenerateField(Structs.ColumnDefinition columnDefinition, Structs.Definition versionDefinition)
        {
            string fieldName = SanitizeFieldName(versionDefinition.name);
            string fieldType;
            switch (columnDefinition.type)
            {
                case "int":
                    fieldType = $"{(IsUnsignedField(versionDefinition, false) ? "u" : "")}";
                    switch (versionDefinition.size)
                    {
                        case 8:
                            fieldType = $"{(IsUnsignedField(versionDefinition, false) ? "" : "s")}byte";
                            break;
                        case 16:
                            fieldType += "short";
                            break;
                        case 32:
                            fieldType += "int";
                            break;
                        case 64:
                            fieldType += "long";
                            break;
                        default:
                            break;
                    }
                    break;
                case "locstring":
                case "string":
                    fieldType = "string";
                    break;
                case "float":
                    fieldType = "float";
                    break;
                default:
                    fieldType = "undefined";
                    break;
            }

            return new FieldValue(fieldType, fieldName, versionDefinition.arrLength, versionDefinition.isID, versionDefinition.isNonInline, versionDefinition.isRelation);
        }

        private string GetExpansionNameForBuild(Build[] builds)
        {
            foreach (Build build in builds)
            {
                if (build.build == buildNumber)
                {
                    switch (build.expansion)
                    {
                        case 1: return build.major > 12 ? "ClassicEra" : "Classic";
                        case 2: return build.major > 4 ? "TheBurningCrusadeClassic" : "TheBurningCrusade";
                        case 3: return build.major > 3 ? "WrathOfTheLichKingClassic" : "WrathOfTheLichKing";
                        case 4: return build.major > 3 ? "CataclysmClassic" : "Cataclysm";
                        case 5: return "MistsOfPandaria";
                        case 6: return "WarlordsOfDraenor";
                        case 7: return "Legion";
                        case 8: return "BattleForAzeroth";
                        case 9: return "Shadowlands";
                        case 10: return "Dragonflight";
                        case 11: return "TheWarWithin";
                        case 12: return "Midnight";
                        default:
                            break;
                    }
                    break;
                }
            }

            return "UnknownExpansion";
        }
    }
}
