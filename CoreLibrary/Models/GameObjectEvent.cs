using Core.Enums;

namespace Core.Models
{
    public record GameObjectEvent(
        string ObjectId,
        GameObjectEventTypeEnum EventType,
        DateTime Timestamp
    );
}
