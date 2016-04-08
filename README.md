[![Build status](https://ci.appveyor.com/api/projects/status/co28ei02qcpln54e/branch/master?svg=true)](https://ci.appveyor.com/project/sayedihashimi/mutant-chicken/branch/master)

# Overview
Mutant Chicken is the next iteration of the replacement capabilities built in to [Template Builder](https://github.com/ligershark/template-builder) and will eventually be used as the replacement engine in [SideWaffle](https://github.com/ligershark/side-waffle), [PecanWaffle](https://github.com/ligershark/pecan-waffle) and DotNetWaffle.

Mutant Chicken is a library for manipulating streams, including operations to replace values, include/exclude regions and process `if`/`else if`/`else`/`end if` style statements.

#Getting Started
_TODO: Add a getting started section_

#Roadmap
* Orchestration
 * Take in a file manifest (include/exclude globbing patterns paired to configurations)
 * Run global operations on the manifest
 * For each file that matches the manifest's resulting patterns, use the aggregated configuration to create modified copies the contents of the files
* Integration packs for each of the Waffles
 * Given that the Waffles are already widely used, integration packs for each of the Waffles will be provided here so that no substantial changes will be required to use Mutant Chicken
* Additional Operations
 * Suggestions welcome!