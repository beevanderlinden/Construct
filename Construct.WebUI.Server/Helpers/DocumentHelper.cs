using CommonLibrary.Models;

namespace Construct.WebUI.Server.Helpers
{
    public class DocumentHelper
    {
        public static void SetDocumentLabel(List<LabelWithStringValue> labels, string label, string value)
        {
            var existing = labels.FirstOrDefault(l =>
                string.Equals(l.Label, label, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.StringValue = value; // aanpassen
            }
            else
            {
                labels.Add(new LabelWithStringValue(label, value)); // toevoegen
            }
        }
    }
}
