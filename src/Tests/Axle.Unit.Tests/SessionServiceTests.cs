using System.Linq;
using System.Threading.Tasks;
using Axle.Contracts;
using Axle.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Axle.Unit.Tests
{
    public class SessionServiceTests
    {
        private readonly Mock<ITokenRevocationService> mockTokenRevocationService = new Mock<ITokenRevocationService>();
        private readonly Mock<INotificationService> mockNotificationService = new Mock<INotificationService>();
        private readonly Mock<IActivityService> mockActivityService = new Mock<IActivityService>();
        private readonly Mock<IAccountsService> mockAccountService = new Mock<IAccountsService>();
        private readonly Mock<ILogger<SessionService>> mockLogger = new Mock<ILogger<SessionService>>();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BeginSession_Same_Device_Shares_Existing_Session(bool isSupportUser)
        {
            var sut = GetSut();

            var session1 = await sut.BeginSession("", "", "", "", isSupportUser, "device1");

            var session2 = await sut.BeginSession("", "", "", "", isSupportUser, "device1");
            
            Assert.Equal(session1.SessionId, session2.SessionId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BeginSession_Different_Device_Creates_New_Session(bool isSupportUser)
        {
            var sut = GetSut();

            var session1 = await sut.BeginSession("", "", "", "", isSupportUser, "device1");

            var session2 = await sut.BeginSession("", "", "", "", isSupportUser, "device2");
            
            Assert.NotEqual(session1.SessionId, session2.SessionId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BeginSession_Same_Device_Adds_Token_To_The_List(bool isSupportUser)
        {
            var sut = GetSut();

            var session1 = await sut.BeginSession("", "", "", "token1", isSupportUser, "device1");
            
            var session2 = await sut.BeginSession("", "", "", "token2", isSupportUser, "device1");

            Assert.Equal(session1.SessionId, session2.SessionId);
            Assert.Equal(2, session2.AccessTokens.Length);
            Assert.Contains("token1", session2.AccessTokens);
            Assert.Contains("token2", session2.AccessTokens);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TerminateSession_Revokes_All_Access_Tokens_If_Not_SignOut(bool isSupportUser)
        {
            var sut = GetSut();
            
            await sut.BeginSession("", "", "", "token1", isSupportUser, "device1");
            
            var session2 = await sut.BeginSession("", "", "", "token2", isSupportUser, "device1");

            await sut.TerminateSession(session2, SessionActivityType.ManualTermination);

            mockTokenRevocationService.Verify(
                x => x.RevokeAccessToken(It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(2));
        }
        
        public ISessionService GetSut() =>
            new SessionService(new MockedSessionRepository(),
                mockTokenRevocationService.Object,
                mockNotificationService.Object,
                mockActivityService.Object,
                mockAccountService.Object,
                mockLogger.Object);
    }
}