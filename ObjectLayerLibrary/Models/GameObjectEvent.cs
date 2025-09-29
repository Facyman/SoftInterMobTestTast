using ObjectLayerLibrary.Enums;

namespace ObjectLayerLibrary.Models
{
    public record GameObjectEvent(
        string ObjectId,
        GameObjectEventTypeEnum EventType,
        DateTime Timestamp
    );
}
