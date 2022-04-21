using Identity.DBContext.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.Test
{
  public class StringExtensionsTests
  {
    [Theory]
    [InlineData("TestCamelCase","test_camel_case")]
    [InlineData("TEST_UPPER_CASE","test_upper_case")]
    [InlineData("testwithoutanycase", "testwithoutanycase")]
    [InlineData("test_with_lower_case", "test_with_lower_case")]
    [InlineData("TEST45_32_OO", "test45_32_oo")]
    [InlineData("TESTAc45_32_OO", "test_ac45_32_oo")]
    public void Test_ToUnderScoreCase(string valueToTest, string expected)
    {
      Assert.Equal(expected, valueToTest.ToUnderscoreCase());
    }
  }
}
