using Moq;
using StageTix.Core.Interfaces.Repositories;

namespace StageTix.Tests.TestHelpers;

public static class UnitOfWorkMock
{
    /// <summary>
    /// A unit of work whose transactions simply run the work they are given,
    /// so the service logic inside the transaction is really executed by the test.
    /// </summary>
    public static Mock<IUnitOfWork> Create()
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<int>> work, CancellationToken ct) => work(ct));

        unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> work, CancellationToken ct) => work(ct));

        return unitOfWork;
    }
}
