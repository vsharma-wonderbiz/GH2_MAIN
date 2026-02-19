using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class TagType
    {
        public int TagTypeId { get; private set; }  
        public string TagName { get; private set; }

        private TagType() { }
        public TagType(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("TagName is required", nameof(tagName));

            this.TagName = tagName;
        }

        public void UpdateTagName(string newTagName)
        {
            if (string.IsNullOrWhiteSpace(newTagName))
                throw new ArgumentException("TagName cannot be empty", nameof(newTagName));

            this.TagName = newTagName;
        }

        public ICollection<Tag> Tags { get; private set; } = new List<Tag>();

    }
}

