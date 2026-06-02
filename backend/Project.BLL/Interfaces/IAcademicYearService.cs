using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IAcademicYearService
{
    Task<OperationResult<AcademicYearDto>>              CreateAcademicYearAsync(CreateAcademicYearRequest request);
    Task<OperationResult<AcademicYearDto>>              UpdateAcademicYearAsync(UpdateAcademicYearRequest request);
    Task<OperationResult>                               DeleteAcademicYearAsync(int id);
    Task<OperationResult<AcademicYearDto>>              GetAcademicYearByIdAsync(int id);
    Task<OperationResult>                               SetCurrentAcademicYearAsync(int id);
    Task<OperationResult<AcademicYearDto>>              GetCurrentAcademicYearAsync();
    Task<OperationResult<IEnumerable<AcademicYearDto>>> GetAllAcademicYearsAsync();
}
