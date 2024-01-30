# MW3-MapEditor
 MapEditor for TeknoMW3

## Usage
 Join server and make sure to use `!IAdmin` command to become admin!
 This command can only be triggered **ONCE** so make sure no one else uses it first or you will have you delete `scripts/MapEdit/Admins.json` retry.
 Each map edits are saved in `scripts/MapEdit/[MAPNAME].json`.
 ### Commands list:
 ```
 !IAdmin
 !/help [command]
 !version
 !/admins
 !fly
 !removeedit (<id> | last)
 !addadmin <player>
 !removeadmin <player>
 !cmd <params>
 !model <model> (<param>:<value>)
 !ramp
 !hramp
 !wall
 !hwall
 !floor
 !hfloor
 !tp
 !htp
 !elevator
 !helevator
 !door [Size:<size>] [Heigth:<heigth>] [Health:<health>]
 ```
 Commands starting with `h` means its spawned hidden as for `!hramp`, `!hwall`, `!htp` and so on.