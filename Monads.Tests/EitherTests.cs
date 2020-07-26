using System;
using NUnit.Framework;

namespace Monads.Tests
{
  public class EitherTests
  {
    private const string ErrorMessage = nameof(ErrorMessage);
    private Either<Exception, int> error;
    private readonly InvalidOperationException exception = new InvalidOperationException(ErrorMessage);
    private Either<Exception, int> value;

    [SetUp]
    public void Setup()
    {
      error = exception;
      value = 100;
    }

    [Test]
    public void Fold_Error_ShouldInvokeLeftFunc()
    {
      var success = error.Fold(ex => false, val => true);

      Assert.IsFalse(success);
    }

    [Test]
    public void Fold_Value_ShouldInvokeRightFunc()
    {
      var success = value.Fold(ex => false, val => true);

      Assert.IsTrue(success);
    }

    [Test]
    public void Map1_Error_ShouldKeepLeft()
    {
      var result = error.Map(val => val * 2m);

      Assert.AreEqual(new Either<Exception, decimal>(exception), result);
    }

    [Test]
    public void Map2_Value_ShouldInvokeFunc()
    {
      var result = value.Map(val => val * 2m);

      Assert.AreEqual(new Either<Exception, decimal>(200m), result);
    }

    [Test]
    public void Map2_Error_ShouldInvokeLeftFunc()
    {
      var result = error.Map(ex => ex.Message, val => val * 2m);

      Assert.AreEqual(new Either<string, decimal>(ErrorMessage), result);
    }

    [Test]
    public void Map2_Value_ShouldInvokeRightFunc()
    {
      var result = value.Map(ex => ex.Message, val => val * 2m);

      Assert.AreEqual(new Either<string, decimal>(200m), result);
    }

    [Test]
    public void Bind_Error_ShouldKeepLeft()
    {
      var result = error.Bind(IsPositive);

      Assert.AreEqual(new Either<Exception, bool>(exception), result);
    }

    [Test]
    public void Bind_Error_ShouldInvokeFunc()
    {
      var result = value.Bind(IsPositive);

      Assert.AreEqual(new Either<Exception, bool>(true), result);
    }

    [Test]
    public void Chaining_Error()
    {
      // map value to floating point and negate it, bind to square-root function, and get either the error message
      var result = value
        .Map(x => -(float)x) 
        .Bind(Sqrt)
        .Map(ex => ex.Message, val => (int)val);
      
      Assert.AreEqual(new Either<string, int>(ErrorMessage), result);
    }

    [Test]
    public void Chaining_Value()
    {
      // map value to floating point, bind to square-root function, and get the integer square-root
      var result = value
        .Map(x => (float)x)
        .Bind(Sqrt)
        .Map(ex => ex.Message, val => (int)val);

      Assert.AreEqual(new Either<string, int>(10), result);
    }

    private Either<Exception, bool> IsPositive(int x)
    {
      if (x == 0) { return exception; }
      return x > 0;
    }

    // Function that returns either the square-root or an error
    private Either<Exception, float> Sqrt(float x)
    {
      var sqrt = MathF.Sqrt(x);
      if (float.IsNaN(sqrt))
      {
        return exception;
      }

      return sqrt;
    }
  }
}