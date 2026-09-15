#!/usr/bin/env python3

"""
Generate C# source Settings.Generated.cs from default-config.json.

Usage:
    python3 Generate_Settings_cs.py
"""

import sys
import json

def main() -> int:

    with open('default-config.json') as f:
        data = json.load(f)

    settings = data.get("settings")
    if not isinstance(settings, dict):
        raise ValueError("Unable to load settings")

    lines = [
        "// NOTE: This file is auto-generated.",
        "// See Generate_Settings_cs.py for details.",
        "",
        "using System.ComponentModel;",
        "",
        "namespace OWMixedInput;",
        ""
    ]

    struct = [
        "public record struct Settings {"
    ]

    extensions = [
        "public static class SettingsExtensions {"
    ]

    for (key, value) in settings.items():
        if not isinstance(value, dict):
            raise ValueError(f'Unsupported value for setting {key}. Expected dictionary')

        # separators and labels don't correspond to settings
        if value.get("type") in ("separator", "label"):
            continue

        if not key.isascii() or not key.isalpha():
            raise ValueError(f'Invalid setting name: {key}')
        if not key[0].isupper():
            raise ValueError(f'Invalid setting name {key}. Must start with an upper case letter.')
        
        # The field name is the key name with the first letter lowercased.
        # This avoids the enum type and the field having the same name.
        field = key[0].lower() + key[1:]

        match value.get("type"):
            case "toggle":
                struct.extend([
                    f'    [Description("{key}")]',
                    f'    public bool {field};',
                    f''
                ])
            case "number":
                struct.extend([
                    f'    [Description("{key}")]',
                    f'    public float {field};',
                    f''
                ])

            case "selector":
                options = value.get("options")
                if not isinstance(options, list):
                    raise ValueError(f'Invalid options for setting {key}')

                if not value.get("value") in options:
                    print(f'Warning: unknown value "{value.get("value")}" for setting {key}', file=sys.stderr)

                option_names = value.get("option_names")
                if option_names is None:
                    option_names = {}
                if not isinstance(option_names, dict):
                    raise ValueError(f'Invalid option_names for setting {key}. Expected dictionary')

                def option_name(option: str) -> str:
                    name = option_names.get(option, option)

                    if not name.isascii() or not name.isalpha():
                        if not "option_names" in value:
                            print('Error: invalid option name. Consider using `option_names`', file=sys.stderr)
                        raise ValueError(f'Invalid option name "{name}" for setting {key}.')

                    return name

                options = { option: option_name(option) for option in options }

                struct.extend([
                    f'    public enum {key} {{',
                ])

                for (option, name) in options.items():
                    struct.extend([
                        f'        [Description("{option}")]',
                        f'        {name},'
                    ])

                struct.extend([
                    f'    }}',
                    f'    [Description("{key}")]',
                    f'    public {key} {field};',
                    f''
                ])

                match_args = ", ".join((f'TResult {name}' for (option, name) in options.items()))

                extensions.extend([
                    f'    public static TResult Match<TResult>(this Settings.{key} {field}, {match_args})',
                    f'        => {field} switch {{',
                ])

                for (option, name) in options.items():
                    extensions.extend([
                        f'            Settings.{key}.{name} => {name},'
                    ])

                extensions.extend([
                    "            _ => throw new Unreachable()",
                    "        };",
                    ""
                ])

                extensions.extend([
                    f'    extension(Settings.{key} {field}) {{'
                ])

                for (option, name) in options.items():
                    extensions.extend([
                        f'        public bool is{name} => {field} == Settings.{key}.{name};'
                    ])

                extensions.extend([
                    "    }",
                    ""
                ])

            case _:
                raise ValueError(f'Unsupported setting type "{value.get("type")}" for setting {key}')

    struct.extend([
        "}",
        ""
    ])

    extensions.extend([
        "}",
        ""
    ])

    lines.extend(struct)
    lines.extend(extensions)

    with open("OWMixedInput/Settings.Generated.cs", mode='w', encoding='utf-8') as f:
        f.write('\n'.join(lines))

    return 0

if __name__ == "__main__":
    sys.exit(main())
