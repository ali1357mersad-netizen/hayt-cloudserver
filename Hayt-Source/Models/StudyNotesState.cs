using System;
using System.Collections.Generic;

namespace Hayt.Models;

public sealed class StudyNotesState
{
    public DateTime LastSavedAt { get; set; } = DateTime.Now;

    public List<StudyNote> Notes { get; set; } = new();

    public static StudyNotesState Empty()
    {
        return new StudyNotesState
        {
            LastSavedAt = DateTime.Now,
            Notes = new List<StudyNote>()
        };
    }
}