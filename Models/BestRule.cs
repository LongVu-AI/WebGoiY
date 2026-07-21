using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebGoiY.Models;

[Table("best_rules")]
public partial class BestRule
{
    [Column("antecedents")]
    public string Antecedents { get; set; } = null!;

    [Column("consequents")]
    public string Consequents { get; set; } = null!;

    [Column("confidence")]
    public float Confidence { get; set; }

    [Column("lift")]
    public float Lift { get; set; }
}