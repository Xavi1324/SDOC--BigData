using SDOC.Domain.Entities.csv;

namespace SDOC.Application.Interfaces.IRepository
{
    public interface IInternalSurveySourceRepository
    {
        Task<IReadOnlyList<SurveyCsv>> GetAllAsync();
    }
}
