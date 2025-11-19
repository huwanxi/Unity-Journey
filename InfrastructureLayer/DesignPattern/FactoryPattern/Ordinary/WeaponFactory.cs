using System;

public class WeaponFactory
{
    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Rifle
    }

    public static IWeapon CreateWeapon(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol:
                return new Pistol();
            case WeaponType.Shotgun:
                return new Shotgun();
            case WeaponType.Rifle:
                return new Rifle();
            default:
                throw new ArgumentException($"Î´ÖªÎäÆ÷ÀàÐÍ: {type}");
        }
    }
}