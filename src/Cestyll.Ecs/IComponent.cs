using Cestyll.Ecs.Attachments;
using Cestyll.Ecs.Inboxes;
using Cestyll.Ecs.Tags;

namespace Cestyll.Ecs;

// Component marker, so passing a tag or unmarked type where a component belongs won't compile.
public interface IComponent { }

// C# can't forbid two markers on a type, so the first store made for it checks.
internal static class Markers
{
    internal static void RequireOne(Type type)
    {
        var kinds = (typeof(IComponent).IsAssignableFrom(type) ? 1 : 0)
            + (typeof(ITag).IsAssignableFrom(type) ? 1 : 0)
            + (typeof(IAttachment).IsAssignableFrom(type) ? 1 : 0)
            + (typeof(IMessage).IsAssignableFrom(type) ? 1 : 0);
        if (kinds > 1)
        {
            throw new ArgumentException(
                $"{type.Name} implements more than one of IComponent, ITag, IAttachment and IMessage; "
                + "a type is one kind");
        }
    }
}
