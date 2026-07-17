using System;
using System.Collections.Generic;

namespace WebGoiY.Models;

public partial class BestRule
{
    public string Antecedents { get; set; } = null!;

    public string Consequents { get; set; } = null!;

    public float Confidence { get; set; }

    public float Lift { get; set; }
}
