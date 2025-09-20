using System.Text.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json.Nodes;

namespace ParkSpotTLV.Infrastructure.Seeding {
    // Loads simple FeatureCollections (Polygon/MultiPolygon; LineString) from GeoJSON files.
    // Returns tuples of (Geometry, PropertiesJson) so the caller can map fields.
    public static class GeoJsonLoader {
        private static readonly GeoJsonReader _reader = new GeoJsonReader();

        public static IEnumerable<(Geometry geom, JsonObject props)> LoadFeatures(string path) {
            if (!File.Exists(path))
                throw new FileNotFoundException($"GeoJSON seed file not found: {path}");

            var text = File.ReadAllText(path);
            // Accept either Feature or FeatureCollection
            var node = JsonNode.Parse(text) as JsonObject
                       ?? throw new InvalidDataException("Invalid GeoJSON root.");

            var type = node["type"]?.GetValue<string>();
            if (type == "FeatureCollection") {
                var features = node["features"]?.AsArray()
                              ?? throw new InvalidDataException("FeatureCollection missing 'features'.");
                foreach (var f in features.OfType<JsonObject>())
                    yield return ParseFeature(f);
            } else if (type == "Feature") {
                yield return ParseFeature(node);
            } else {
                // Plain geometry (no properties)
                var geom = _reader.Read<Geometry>(text);
                yield return (geom, new JsonObject());
            }
        }

        private static (Geometry geom, JsonObject props) ParseFeature(JsonObject feature) {
            var geomNode = feature["geometry"] as JsonObject
                           ?? throw new InvalidDataException("Feature missing geometry.");
            var geomText = geomNode.ToJsonString();
            var geom = _reader.Read<Geometry>(geomText);
            var props = feature["properties"] as JsonObject ?? new JsonObject();
            return (geom, props);
        }
    }
}
