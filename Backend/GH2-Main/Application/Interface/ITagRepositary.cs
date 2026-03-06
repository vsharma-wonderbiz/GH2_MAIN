using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface ITagRepositary
    {
        Task<Tag?> GetTagNameById(int tagId);
        Task<List<Tag>> GetTagsByNames(List<string> tagNames);
        Task<List<Tag>> GetAllKpiTags();


       
    }
}
