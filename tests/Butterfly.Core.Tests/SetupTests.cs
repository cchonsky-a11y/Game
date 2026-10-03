using Butterfly.Core;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class SetupTests
    {
        [Fact]
        public void CoreLibraryIsReferenced()
        {
            Assert.Equal("P1 Playable Game Structure", CoreInfo.Milestone);
        }
    }
}
