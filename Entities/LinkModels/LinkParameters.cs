using Microsoft.AspNetCore.Http;
using Shared.Paging;

namespace Entities.LinkModels
{
    public record LinkParameters(EmployeeParameters EmployeeParameters, HttpContext Context);


}
