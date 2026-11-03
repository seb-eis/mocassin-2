# Glossary, Notation, and Units

## Monte Carlo terms

| Term | Meaning |
| --- | --- |
| **MMC** | Metropolis Monte Carlo; samples equilibrium configurations without advancing time. |
| **KMC** | Kinetic Monte Carlo; selects transitions by rate and advances simulated time. |
| **MCSP** | Monte Carlo steps per mobile particle. |
| **Pre-run** | KMC equilibration phase before the main run. |
| **Normalization** | Factor that scales fixed and automatic rates to a common time base. |

## Model terms

| Term | Meaning |
| --- | --- |
| **P1** | The symmetry-expanded cell with every position listed explicitly. |
| **Stable site** | A reference site that hosts particles and participates in the energy model. |
| **Unstable site** | A transition-state position; defaults to void. |
| **State change** | A donor/acceptor particle replacement on one position. |
| **Chain** | An ordered sequence of state-change groups with dynamic/static linkers. |

## Units

| Quantity | Unit |
| --- | --- |
| Length | Ångström (Å) in model/input; SI in transport output |
| Energy | electronvolt (eV) in model and tables; thermal units internally after per-job conversion |
| Temperature | kelvin (K) |
| Charge | elementary charge |
| Time | second (s) |
| Frequency | hertz (Hz) |
| Electric field | volt per metre (V/m) |
| Tracker displacement | fractional until output conversion |

Unit-cell counts use `A*B*C`, never `A+B+C`.
