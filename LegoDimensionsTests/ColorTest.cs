using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsTests
{
    public class ColorTest
    {
        [Fact]
        public void ColorTestBasicSerialization() 
        {
            // Arrange
            var col = Color.Red;

            // Act
            var col2 = Color.ParseHex(col.ToString());

            // Assert
            Assert.Equal(col, col2);
        }

        [Fact]
        public void ColorTestColorSerialization()
        {
            // Arrange
            var col = "Red";

            // Act
            var col2 = Color.FromColorName(col);

            // Assert
            Assert.NotNull(col2);
            Assert.Equal(Color.Red, col2!);
        }

        [Fact]
        public void ColorTestColorCaseSerialization()
        {
            // Arrange
            var col = "rEd";

            // Act
            var col2 = Color.FromColorName(col);

            // Assert
            Assert.NotNull(col2);
            Assert.Equal(Color.Red, col2!);
        }

        [Fact]
        public void ColorEqualTest()
        {
            // Arrange
            var col1 = Color.Red;
            var col2 = Color.Red;

            Assert.Equal(col1, col2);
            Assert.True(col1 == col2);
        }
    }
}

