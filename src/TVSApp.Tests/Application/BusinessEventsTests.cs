using System.Reflection;
using System.Text.RegularExpressions;
using TVS_App.Application.Observability;

namespace TVSApp.Tests.Application;

[TestClass]
public class BusinessEventsTests
{
    private static List<string> AllEvents() => typeof(BusinessEvents)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name != nameof(BusinessEvents.MeterName))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    [TestMethod]
    public void Events_AreUniqueAndFollowResourceActionFormat()
    {
        var events = AllEvents();

        Assert.IsTrue(events.Count >= 25);
        Assert.AreEqual(events.Count, events.Distinct().Count());
        foreach (var name in events)
            Assert.IsTrue(Regex.IsMatch(name, "^[a-z_]+\\.[a-z_]+$"), name);
    }

    [TestMethod]
    public void Catalog_HasTheWhatsAppLifecycle()
    {
        CollectionAssert.IsSubsetOf(
            new[] { "whatsapp.enqueued", "whatsapp.sent", "whatsapp.failed" },
            AllEvents());
    }
}
