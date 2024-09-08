using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This extension class contains a function that returns the positive only modulo of a value.
/// </summary>
public static class PositiveModuloLibrary
{
    /// <summary>
    /// Calculate the modulo for a given modulus.
    ///
    /// It doesn't give strictly positive modulos - It will have the same sign as <paramref name="modulus"/>. In most cases this is positive, hence the name.
    /// <example><code>
    /// var result = 7.PositiveModulo(4);
    /// Assert.AreEqual(result, 3);
    /// Assert.AreEqual(-5.PositiveModulo(2), 1);
    ///
    /// Assert.AreEqual( 3.PositiveModulo(3), 0);
    /// Assert.AreEqual( 2.PositiveModulo(3), 2);
    /// Assert.AreEqual( 1.PositiveModulo(3), 1);
    /// Assert.AreEqual( 0.PositiveModulo(3), 0);
    /// Assert.AreEqual(-1.PositiveModulo(3), 2);
    /// Assert.AreEqual(-2.PositiveModulo(3), 1);
    /// </code></example>
    /// </summary>
    public static int PositiveModulo(this int self, int modulus)
    {
        return (self % modulus + modulus) % modulus;
    }
}

}
