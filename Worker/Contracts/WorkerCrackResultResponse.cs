using System.Xml.Serialization;

namespace Worker.Contracts;

[XmlRoot("WorkerCrackResult")]
public sealed class WorkerCrackResultResponse
{
    [XmlElement("RequestId")]
    public string RequestId { get; set; } = string.Empty;

    [XmlElement("PartNumber")]
    public int PartNumber { get; set; }

    [XmlArray("Words")]
    [XmlArrayItem("Word")]
    public List<string> Words { get; set; } = [];
}
