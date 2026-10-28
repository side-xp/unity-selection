# Installation

## Dependencies

🔗 Requires our [Core library package](https://github.com/side-xp/unity-core)

Since our packages are not published on a registry, Unity can't fetch this dependency automatically: install the Core library first, using one of the options below, then install this package the same way.

## Option 1: Using the Package Manager

1. In your *Unity* project, go to `Window > Package Management > Package Manager` (or `Window > Package Manager` for *Unity 6.0-*)
2. Click on the *+* icon in the top-left corner, and select *Install package from Git URL...*
3. In the text field, enter the URL to this package's repository (including the `*.git` extension), and click *Install*
4. Wait for Unity to get the files, and you're ready to go!

> Tip: if you need to use a specific version of this package for your project, add `#<tag-name>` to the URL before clicking on the *Install* button.

## Option 2: Extracting archive manually

1. Go to this project's `/releases` list
2. Download the ZIP file archive of your desired version
3. Extract the content of that archive into the `Packages/` folder of your Unity project
4. Wait for Unity to reload the solution, and you're ready to go!

> Tip: to avoid any path issue, make sure the folder that contains the package content has the same name as the `name` property defined in its `package.json` file.

## Next steps

Once installed, open the Selection window from `Tools > Sideways Experiments > Selection > Selection Window`, and check the [Manual](./guides/manual.md) to learn how to use it.
