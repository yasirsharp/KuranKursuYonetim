using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Results;

namespace HafizTakip.Business.Tests.Common;

/// <summary>Testler için Unit of Work sahteleri.</summary>
public static class UnitOfWorks
{
    /// <summary>
    /// Transaction'ı taklit etmeden işi doğrudan çalıştırır; SaveChangesAsync çağrıları sayılabilsin diye mock döner.
    /// </summary>
    public static Mock<IUnitOfWork> PassThrough()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<IResult>>>()))
            .Returns((Func<Task<IResult>> work) => work());
        unitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        return unitOfWork;
    }
}
