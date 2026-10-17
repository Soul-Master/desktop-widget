# Huawei Solar library attribution

This C# port originally used register definitions, numeric decoding behavior and
connection timing defaults from the locally supplied `huawei-solar-lib-3.0.7`.
The current register catalog, types, gains, units and access modes are ported
from the reviewed Huawei SUN2000MA SPC106 document identified in
`REGISTER-SOURCES.md`. Numeric sentinel conventions, status labels and connection
defaults retain upstream behavior. The retained AGPL license is unchanged.

- Upstream project: https://github.com/wlcrs/huawei-solar-lib
- Authors listed in the bundled `pyproject.toml`: wlcrs and Emil Vanherp.
- License supplied by upstream: GNU Affero General Public License, version 3.
  An unmodified copy of the supplied license is included as `LICENSE.md`.

Ported source material:

- `src/huawei_solar/register_names.py`: symbolic register names.
- `src/huawei_solar/registers.py`: selected SUN2000, meter and storage definitions.
- `src/huawei_solar/register_definitions/number.py`: signed/unsigned decoding,
  gains and invalid-value sentinels.
- `src/huawei_solar/register_values.py`: SUN2000 operating states and meter status codes.
- `src/huawei_solar/modbus_client.py`: default TCP settings and pacing values.

The C# attribute model binding, TCP transport, explicit write API, sample models and loopback
checks were added for this port. The port is limited to the fields documented in
`README.md`; it does not claim feature parity with the upstream project.
