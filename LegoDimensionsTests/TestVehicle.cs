using LegoDimensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegoDimensionsTests
{
    public class TestVehicle
    {
        [Fact]
        public void TestRebuild()
        {
            // Arrange
            var vehicle0 = new Vehicle(0, "name", "world", null);            
            var vehicle1 = new Vehicle(1, "name", "world", null);            
            var vehicle2 = new Vehicle(2, "name", "world", null);            
            var vehicle3 = new Vehicle(3, "name", "world", null);            
            var vehicle155 = new Vehicle(155, "name", "world", null);            

            // Act            
            // Assert
            Assert.Equal(VehicleRebuild.First, vehicle0.Rebuild);
            Assert.Equal(VehicleRebuild.Second, vehicle1.Rebuild);
            Assert.Equal(VehicleRebuild.Third, vehicle2.Rebuild);
            Assert.Equal(VehicleRebuild.First, vehicle3.Rebuild);
            Assert.Equal(VehicleRebuild.First, vehicle155.Rebuild);
        }
    }
}
