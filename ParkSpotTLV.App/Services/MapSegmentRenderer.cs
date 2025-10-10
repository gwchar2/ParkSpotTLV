using Microsoft.Maui.Controls.Maps;
using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Map;

namespace ParkSpotTLV.App.Services;


// Handles rendering of parking segments on the map
public class MapSegmentRenderer
{
    // Color constants
    private const string COLOR_FREE_PARKING = "#40dd7c";      // Green
    private const string COLOR_PAID_PARKING = "#4769b9";      // Blue
    private const string COLOR_LIMITED_PARKING = "#f2d158";   // Yellow
    private const string COLOR_RESTRICTED_PARKING = "#f15151"; // Red
    private const string COLOR_UNKNOWN = "#808080";            // Gray

    // Rendering constants
    private const int MAX_SEGMENTS_TO_RENDER = 500;
    private const int SEGMENT_STROKE_WIDTH = 5;

  
    // Renders segments on the map with filtering based on session preferences
    // Returns a dictionary mapping SegmentId -> StreetName for all rendered segments
    public Dictionary<SegmentResponseDTO, string> RenderSegments(Microsoft.Maui.Controls.Maps.Map map, GetMapSegmentsResponse segmentsResponse, Session? session)
    {
        var segmentToStreet = new Dictionary<SegmentResponseDTO, string>();

        if (map == null || segmentsResponse?.Segments == null)
        {
            System.Diagnostics.Debug.WriteLine("RenderSegments: Invalid parameters");
            return segmentToStreet;
        }

        // Clear existing map elements and force garbage collection
        ClearMapElements(map);

        int renderedCount = 0;

        // Draw each segment
        foreach (var segment in segmentsResponse.Segments)
        {
            if (renderedCount >= MAX_SEGMENTS_TO_RENDER)
            {
                System.Diagnostics.Debug.WriteLine($"Reached max segment limit ({MAX_SEGMENTS_TO_RENDER}), stopping render");
                break;
            }

            // Skip based on filter settings from session
            if (ShouldSkipSegment(segment, session))
                continue;

            try
            {
                var polyline = CreatePolylineFromSegment(segment);
                if (polyline != null)
                {
                    map.MapElements.Add(polyline);
                    renderedCount++;

                    // Add street name and segment ID to dictionary (prefer English, fallback to Hebrew)
                    var streetName = !string.IsNullOrEmpty(segment.NameEnglish)
                        ? segment.NameEnglish
                        : segment.NameHebrew ?? "Unknown";

                    segmentToStreet[segment] = streetName;
                }
            }
            catch (OutOfMemoryException)
            {
                System.Diagnostics.Debug.WriteLine($"Out of memory after rendering {renderedCount} segments");
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering segment: {ex.Message}");
                // Continue to next segment
            }
        }

        var uniqueStreets = segmentToStreet.Values.Distinct().Count();
        System.Diagnostics.Debug.WriteLine($"Successfully rendered {renderedCount} segments on {uniqueStreets} streets");
        return segmentToStreet;
    }

   
    // Clears all map elements and forces garbage collection
    private void ClearMapElements(Microsoft.Maui.Controls.Maps.Map map)
    {
        try
        {
            map.MapElements.Clear();

            // Force garbage collection to free up memory before loading new segments
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing map elements: {ex.Message}");
        }
    }

    // Determines if a segment should be skipped based on session filters
    private bool ShouldSkipSegment(SegmentResponseDTO segment, Session? session)
    {
        if (session == null)
            return false; // Show all if no session

        return segment.Group switch
        {
            "NOPARKING" => !session.ShowNoParking,
            "PAID" => !session.ShowPaid,
            "FREE" => !session.ShowFree,
            "RESTRICTED" => !session.ShowRestricted,
            _ => false
        };
    }

    // Creates a polyline from segment GeoJSON data
    private Polyline? CreatePolylineFromSegment(SegmentResponseDTO segment)
    {
        var strokeColor = GetColorForSegment(segment.Group);

        // Parse the GeoJSON geometry
        var geometry = segment.Geometry;
        if (!geometry.TryGetProperty("type", out var geoType) || geoType.GetString() != "LineString")
            return null;

        if (!geometry.TryGetProperty("coordinates", out var coordinates))
            return null;

        var line = new Polyline
        {
            StrokeWidth = SEGMENT_STROKE_WIDTH,
            StrokeColor = strokeColor
        };

        foreach (var coordinate in coordinates.EnumerateArray())
        {
            // GeoJSON format is [longitude, latitude]
            double longitude = coordinate[0].GetDouble();
            double latitude = coordinate[1].GetDouble();
            line.Geopath.Add(new Location(latitude, longitude));
        }

        return line;
    }

    // Gets the color for a parking segment group
    private Color GetColorForSegment(string group)
    {
        return group switch
        {
            "FREE" => Color.FromArgb(COLOR_FREE_PARKING),
            "PAID" => Color.FromArgb(COLOR_PAID_PARKING),
            "RESTRICTED" => Color.FromArgb(COLOR_LIMITED_PARKING),
            "NOPARKING" => Color.FromArgb(COLOR_RESTRICTED_PARKING),
            _ => Color.FromArgb(COLOR_UNKNOWN)
        };
    }
}
