using System;

namespace Domain.Entities
{
    public class ExportRequestTags
    {
        public int TagRequestId { get; private set; }
        public int ExportRequestId { get; private set; }
        public string TagName { get; private set; }

        public ExportRequest ExportRequest { get; private set; }
        public ExportRequestTags() { }

        public ExportRequestTags(int exportRequestId, string tagName)
        {
     

            if (exportRequestId <= 0)
                throw new ArgumentException("ExportRequestId must be positive");

            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("TagName cannot be null or empty");

            ExportRequestId = exportRequestId;
            TagName = tagName;
        }

        public void UpdateTagName(string newTagName)
        {
            if (string.IsNullOrWhiteSpace(newTagName))
                throw new ArgumentException("TagName cannot be null or empty");

            TagName = newTagName;
        }
    }
}
