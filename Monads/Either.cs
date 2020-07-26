using System;

namespace Monads
{
  /// <summary>
  /// Either monad representing the result of operation which has only two distinct outcomes.
  /// <para />
  /// When used for error handling, the left value is usually the error result, and the right value the success result (mnemonic: right also means correct).
  /// </summary>
  /// <typeparam name="TLeft">Left/error type</typeparam>
  /// <typeparam name="TRight">Right/success type</typeparam>
  public readonly struct Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>, IEquatable<TRight>
  {
    private readonly TLeft left;
    private readonly TRight right;
    private readonly bool isLeft;

    public Either(TLeft left) : this(left, default!, true) { }

    public Either(TRight right) : this(default!, right, false) { }

    internal Either(TLeft left, TRight right, bool isLeft)
    {
      this.left = left;
      this.right = right;
      this.isLeft = isLeft;
    }

    public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

    public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);

    /// <summary>
    /// Reduces the monad to a single value.
    /// <para />
    /// It accepts two function delegates which handle the case how the left/error value should be mapped,
    /// and how the right/success value should be mapped.
    /// </summary>
    /// <typeparam name="T">Resulting type</typeparam>
    /// <param name="leftFunc">Action to take when the left value is set</param>
    /// <param name="rightFunc">Action to take when the right value is set</param>
    /// <remarks>Also called match or catamorphism in some implementations.</remarks>
    public T Fold<T>(Func<TLeft, T> leftFunc, Func<TRight, T> rightFunc) => 
      isLeft ? leftFunc(left) : rightFunc(right);

    /// <summary>
    /// Maps the right/success value to another value.
    /// <para />
    /// Will only be executed when the left/error condition is not set.
    /// </summary>
    /// <typeparam name="TR">Resulting type</typeparam>
    /// <param name="func">mapping function</param>
    public Either<TLeft, TR> Map<TR>(Func<TRight, TR> func) =>
      Fold(Either.Left<TLeft, TR>, r => Either.Right<TLeft, TR>(func(r)));

    /// <summary>
    /// Maps the right/success value to another value.
    /// <para />
    /// Will only be executed when the left/error condition is not set.
    /// </summary>
    /// <typeparam name="TL">Resulting left/error type</typeparam>
    /// <typeparam name="TR">Resulting right/success type</typeparam>
    /// <param name="leftFunc">Delegate to map the left/error value</param>
    /// <param name="rightFunc">Delegate to map the right/success value</param>
    /// <remarks>Also called bimap in some implementations.</remarks>
    public Either<TL, TR> Map<TL, TR>(Func<TLeft, TL> leftFunc, Func<TRight, TR> rightFunc) =>
      Fold(l => Either.Left<TL, TR>(leftFunc(l)), r => Either.Right<TL, TR>(rightFunc(r)));

    /// <summary>
    /// Allows chaining calls to other methods that also return an Either monad.
    /// <para />
    /// The delegate is only called when the left/error value is not set.
    /// </summary>
    /// <typeparam name="TR">Resulting right type</typeparam>
    /// <param name="func">delegate to call</param>
    /// <remarks>Also called flatMap or chain in some implementations.</remarks>
    public Either<TLeft, TR> Bind<TR>(Func<TRight, Either<TLeft, TR>> func) => Fold(Either.Left<TLeft, TR>, func);

    /// <summary>
    /// Swaps the left and right side.
    /// </summary>
    /// <returns>Swapped monad</returns>
    public Either<TRight, TLeft> Swap() => Fold(Either.Right<TRight, TLeft>, Either.Left<TRight, TLeft>);

    public override int GetHashCode() => isLeft ? left?.GetHashCode() ?? 0 : right?.GetHashCode() ?? 0;

    public bool Equals(Either<TLeft, TRight> other) =>
      isLeft == other.isLeft && (isLeft ? Equals(left, other.left) : Equals(right, other.right));

    public bool Equals(TRight other) => !isLeft && Equals(right, other);

    public override bool Equals(object? obj) => obj switch
    {
      TRight otherValue => Equals(otherValue),
      Either<TLeft, TRight> otherEither => Equals(otherEither),
      _ => false
    };

    public override string ToString()
    {
      return Fold(l => $"Left: {l}", r => $"Right: {r}");
    }
  }

  public static class Either
  {
    /// <summary>
    /// Creates an Either monad for the left/error value.
    /// <para />
    /// This helper method can be used in case where TLeft and TRight are of the same type and the compiler doesn't which implicit operator to use.
    /// </summary>
    /// <typeparam name="TLeft">Left type</typeparam>
    /// <typeparam name="TRight">Right type</typeparam>
    /// <param name="left">Left/error value</param>
    /// <returns>Monad</returns>
    public static Either<TLeft, TRight> Left<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left, default!, true);

    /// <summary>
    /// Creates an Either monad for the right/success value.
    /// <para />
    /// This helper method can be used in case where TLeft and TRight are of the same type and the compiler doesn't which implicit operator to use.
    /// </summary>
    /// <typeparam name="TLeft">Left/error type</typeparam>
    /// <typeparam name="TRight">Right/success type</typeparam>
    /// <param name="right">Right/success value</param>
    /// <returns>Monad</returns>
    public static Either<TLeft, TRight> Right<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(default!, right, false);

    /// <summary>
    /// Invokes a delegate and produces an Either monad with either the exception or the success value.
    /// </summary>
    /// <typeparam name="TException">Type of the exception value</typeparam>
    /// <typeparam name="TValue">Type of the success value</typeparam>
    /// <param name="func">Delegate</param>
    /// <returns>Monad</returns>
    public static Either<TException, TValue> Try<TException, TValue>(Func<TValue> func) where TException : Exception
    {
      try
      {
        return func()!;
      }
      catch (TException e)
      {
        return e;
      }
    }

    /// <summary>
    /// Invokes a delegate and produces an Either monad with either the exception or the success value.
    /// </summary>
    /// <typeparam name="TValue">Type of the success value</typeparam>
    /// <param name="func">Delegate</param>
    /// <returns>Monad</returns>
    public static Either<Exception, TValue> Try<TValue>(Func<TValue> func) => Try<Exception, TValue>(func);
  }
}