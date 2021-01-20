﻿using System.Collections.Generic;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.TestHarness;
using Reductech.EDR.Core.Util;
using static Reductech.EDR.Core.TestHarness.StaticHelpers;

namespace Reductech.EDR.Core.Tests.Steps
{

public partial class FileExtractTests : StepTestBase<FileExtract, Unit>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                "FileExtract",
                new FileExtract
                {
                    ArchiveFilePath = Constant("Foo"),
                    Destination     = Constant("Bar"),
                    Overwrite       = Constant(true)
                },
                Unit.Default
            ).WithFileSystemAction(
                x => x.Setup(a => a.ExtractToDirectory("Foo", "Bar", true)).Returns(Unit.Default)
            );
        }
    }
}

}