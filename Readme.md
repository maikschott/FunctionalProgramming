# Functional error handling with the Either monad for C#

## Background
If a method can't perform its action there are two ways to report this error condition,
which have some drawbacks.

### 1. Throwing an exception
A common approach is to throw an exception. Its disadvantages are that:
a) catching an exception is very expensive and thus should only be used for "exception" circumstances,
never to control the program flow;
b) the caller needs to know that and which exceptions can be thrown and needs to remember to catch them at some point;
c) may introduce lots of boilerplate code especially if there are nested try-catch blocks which can make the actual program flow less legible. 

### 2. Returning an error object/code 
Returning an error object or code means altering a methods signature from:
```csharp
Result Method(string arg);
```

to
```csharp
(int errorcode, Result result) Method(string arg);
```
or
```csharp
int Method(string arg, out Result result); // returns an error code with 0 being success
```

While returning an error object or code is more performant its disadvantages are:
a) the caller needs to remember to evaluate the error object/code and ignoring critical errors may lead to an unstable application later;
b) function would always need to return its actual funtion results and an error object together.
However it's not automatically clear if there could be cases where both an error and a function result are returned,
and if this happens should the function result be treated as a success or failure.
c) unlike exceptions unhandled error objects/codes are not automatically propagated along the callstack, which needs to be done manually and thus can be error-prone. 

## Either monad
A monad is very roughly just a wrapper that enriches a data type with new functionality.

The Either monad is a such a wrapper with following signature:
```csharp
public readonly struct Either<TLeft, TRight> { ... }
```

which represents a result which has only two possibilities.


When used for error handling, the left value is usually the error result, and the right value the success result
(mnemonic: right also means correct).

The "magic" is that these values are not directly exposed but only indirectly via methods,
which on the one hand forces the user to think about both the error case and success case when evaluating the Either monad,
but on the other hand also reduces the need to think about both outcomes when it's not needed.

### Creating the monad
Monads also have a _unit_ operation which is used to construct the Monad.
In this implementation this is done by the constructor or more conveniently by the following implicit operators:
```csharp
public static implicit operator Either<TLeft, TRight>(TLeft left)
public static implicit operator Either<TLeft, TRight>(TRight right)
```

The first operator creates an Either monad from a left value, and
the second operator creates an Either monad from a right value.

### `Fold`
Also called _match_ or _catamorphism_ in some implementations.
```csharp
public T Fold<T>(Func<TLeft, T> leftFunc, Func<TRight, T> rightFunc)
```

This method reduces the monad to a single value. It accepts two function delegates which handle the case
how the left/error value should be mapped, and how the right/success value should be mapped.

### `Map`
The two argument overload is also called _bimap_ in some implementations.
```csharp
public Either<TLeft, TR> Map<TR>(Func<TRight, TR> func)
public Either<TL, TR> Map<TL, TR>(Func<TLeft, TL> leftFunc, Func<TRight, TR> rightFunc)
```

The first overload is used to map the right/success value to another value, and will only be executed when the left/error condition is not set.

The second overload is similar, but also allows handling of the left/error value.

Both overloads return a new Either monad. 

### `Bind`
Also called _flatMap_ or _chain_ in some implementations.
```csharp
public Either<TLeft, TR> Bind<TR>(Func<TRight, Either<TLeft, TR>> func)
```

This method is used to chain calls to other methods that also return an Either monad.
The delegate is only called when the left/error value is not set.

### `Swap`
```csharp
public Either<TRight, TLeft> Swap()
```
Swaps the left and right side, e.g. if the wanted result of a computation is the returned error.

### `Either.Try`
Actually, these could be part of constructing a Try monad, which is a different monad, but also similar to an Either monad with the left value being an exception.

```csharp
public static Either<Exception, TValue> Try<TValue>(Func<TValue> func)
public static Either<TException, TValue> Try<TException, TValue>(Func<TValue> func) where TException : Exception
```

Both methods invoke the provided delegate and return an Either monad,
which either has the exception as the left value in case the delegate threw one,
or the actual delegate result as the right value.

The first overload catches all exceptions, the second overload allows that only a specific exception (or its subclasses) are caught and wrapped in the monad.

Because, like initially stated, catching exceptions is expensive, these should only be used to wrap method over whose signature the user has no control, i.e. framework methods or third-party libary methods.

## Example
<table>
<tr>
<td>With Either monad</td><td>Without</td>
</tr>
<tr valign="top"><td>

```csharp
Either<string, float> ToNumber(string str)
{
  if (float.TryParse(str, out var number)) { return number; }
  return $"Could not convert '{str}' to a number";
}

Either<string, float> Sqrt(float number)
{
  if (number < 0) { return "Square root of a negative number"; }
  return MathF.Sqrt(number);
}

void Main()
{
  var sqrt = Either.Try(Console.ReadLine)   // ReadLine may throw
    .Map(error => error.Message, val => val)
    .Bind(ToNumber)                         // convert to float
    .Bind(Sqrt)                             // calculate square root
    .Fold(error =>          // produce final value and handle errors
      { 
        Console.Error.WriteLine(error);
        return float.NaN;
      },
      val => val);
  Console.WriteLine($"Result: {sqrt}");
}
```
</td><td>

```csharp
float ToNumber(string str)
{
  if (float.TryParse(str, out var number)) { return number; }
  else { throw new InvalidOperationException($"Could not convert '{line}' to a number"); }
}

float Sqrt(float number)
{
  if (number < 0) { throw new ArgumentException("Square root of a negative number"); }
  return MathF.Sqrt(number);
}

void Main()
{
  float sqrt;
  try
  {
    var number = ToNumber(Console.ReadLine());
    sqrt = Sqrt(number);
  }
  catch (Exception ex)
  {
    Console.Error.WriteLine(ex.Message);
    sqrt = float.NaN;
  }
  Console.WriteLine($"Result: {sqrt}");
}
```
</td></tr>
</table>