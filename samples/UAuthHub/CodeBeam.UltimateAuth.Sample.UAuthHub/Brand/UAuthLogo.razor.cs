using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace CodeBeam.UltimateAuth.Sample;

public partial class UAuthLogo : ComponentBase
{
    [Parameter] public UAuthLogoVariant Variant { get; set; } = UAuthLogoVariant.Brand;

    [Parameter] public int Size { get; set; } = 32;

    [Parameter] public string? ShieldColor { get; set; } = "#00072d";
    [Parameter] public string? KeyColor { get; set; } = "#f6f5ae";

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }

    private string BuildStyle()
    {
        if (Variant == UAuthLogoVariant.Mono)
            return $"color: {KeyColor}; {Style}";

        return Style ?? "";
    }

    protected string KeyPath => @"
M120.43,39.44H79.57A11.67,11.67,0,0,0,67.9,51.11V77.37
A11.67,11.67,0,0,0,79.57,89H90.51l3.89,3.9v5.32l-3.8,3.81v81.41H99
v-5.33h13.69V169H108.1v-3.8H99C99,150.76,111.9,153,111.9,153
V99.79h-8V93.32L108.19,89h12.24
A11.67,11.67,0,0,0,132.1,77.37V51.11
A11.67,11.67,0,0,0,120.43,39.44Z

M79.57,48.19h5.84a2.92,2.92 0 0 1 2.92,2.92
v5.84a2.92,2.92 0 0 1 -2.92,2.92
h-5.84a2.91,2.91 0 0 1 -2.91,-2.92
v-5.84a2.91,2.91 0 0 1 2.91,-2.92Z

M79.57,68.62h5.84a2.92,2.92 0 0 1 2.92,2.92
v5.83a2.92,2.92 0 0 1 -2.92,2.92
h-5.84a2.91,2.91 0 0 1 -2.91,-2.92
v-5.83a2.91,2.91 0 0 1 2.91,-2.92Z

M114.59,48.19h5.84a2.92,2.92 0 0 1 2.91,2.92
v5.84a2.91,2.91 0 0 1 -2.91,2.91
h-5.84a2.92,2.92 0 0 1 -2.92,-2.91
v-5.84a2.92,2.92 0 0 1 2.92,-2.92Z

M114.59,68.62h5.84a2.92,2.92 0 0 1 2.91,2.92
v5.83a2.91,2.91 0 0 1 -2.91,2.92
h-5.84a2.92,2.92 0 0 1 -2.92,-2.92
v-5.83a2.92,2.92 0 0 1 2.92,-2.92Z
";
}
