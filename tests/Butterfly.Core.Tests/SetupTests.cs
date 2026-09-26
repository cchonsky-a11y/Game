using Butterfly.Core;
using Xunit;

namespace Butterfly.Core.Tests
{
    public class SetupTests
    {
        [Fact]
        public void CoreLibraryIsReferenced()
        {
            Assert.Equal("P0 Butterfly Test", CoreInfo.Milestone);
        }
    }
}
