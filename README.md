# Block Farm Editor

## 🚀 Beta Trial Notice

**Current Status:** Block Farm Editor is currently in beta and available for a **3-month rolling trial** period. 

After the beta period, the product will transition to a **yearly licensing model**.  Currently targeting a $50 yearly license pre main domain with an increase to $100 once feature complete.

If you want to start building sites with it, signup using the following domain registration hubspot form in order to be contacted when fully released.  [Domain Registration Form](https://40lw0b.share-na2.hsforms.com/2_On6_GUgTRS5rPA5LkRK4Q)

---

## Overview

Block Farm Editor is a visual content editor that enables flexible, block-based content editing. Build reusable content blocks and containers that can be easily managed and rendered throughout your Umbraco website.

## Initial Install

```
dotnet add package BlockFarmEditor.Umbraco
```

This will add a Composition and Data Type of "Block Farm Editor"
To begin using the page builder,
1. Add the composition to your Document Type.
2. Add ```@addTagHelper *, BlockFarmEditor.RCL``` to your **_ViewImports.cshtml** file.
3. Add ```<register-block-farm></register-block-farm>``` to the head section of your layout file.  This tag registers the front end javascript needed when in edit mode.
4. Retrieve the license.  See Licensing.
5. Follow the Quick Start to get started building.  Note especially step 3.

## Licensing. 
Go to the Block Farm Editor settings dashboard.  Select your domain and click (Re)Validate.

## Quick Start
Note, if you are going to package your components in seperate projects, do not add the nuget project ```BlockFarmEditor.Umbraco``` to those projects. 
Instead add 
```
dotnet add package BlockFarmEditor.RCL
```

### 1. Register Block Definitions

Use assembly attributes to register your available blocks and containers:

#### Block Registration & Container Registration
The difference between a container and a block is purely syntactic, allowing for categorization on the front end.

````csharp
// Blocks
[assembly: BlockFarmEditorBlock("my.text.block", "Text Block", 
    PropertiesType = typeof(TextBlockProperties), 
    ViewPath = "~/Views/Blocks/TextBlock.cshtml", 
    Icon = "icon-text")]

[assembly: BlockFarmEditorBlock("my.image.block", "Image Block", 
    PropertiesType = typeof(ImageBlockProperties), 
    ViewComponentType = typeof(ImageBlockViewComponent), 
    Icon = "icon-picture")]

// Containers
[assembly: BlockFarmEditorContainer("my.section.container", "Section Container", 
    PropertiesType = typeof(SectionContainerProperties), 
    ViewPath = "~/Views/Containers/Section.cshtml", 
    Icon = "icon-grid")]

[assembly: BlockFarmEditorContainer("my.complex.section.container", "Complex Section Container", 
    PropertiesType = typeof(ComplexSectionContainerProperties), 
    ViewComponentType = typeof(ComplexSectionContainerViewComponent), 
    Icon = "icon-grid")]
````

### Define Block Properties
Create properties classes that implement IBuilderProperties and use the property attributes:

#### Using Data Types
Using a BlockFarmEditorDataType allows you to control the property editor's config in the backoffice.
````csharp
public class TextBlockProperties : IBuilderProperties
{
    [BlockFarmEditorDataType("Textstring", "Heading")]
    public string? Heading { get; set; }
    
    [BlockFarmEditorDataType("Richtext editor", "Content")]
    public string? Content { get; set; }
}
````

#### Using Property Editors
Using a BlockFarmEditorDataType allows you to control the property editor's config in the backoffice.
````csharp
public class ImageBlockProperties : IBuilderProperties
{
    [BlockFarmEditorPropertyEditor("Umbraco.MediaPicker3", "Image")]
    public MediaWithCrops? Image { get; set; }
    
    [BlockFarmEditorPropertyEditor("Umbraco.TextBox", "Alt Text")]
    public string? AltText { get; set; }
}
````

##### Adding a custom config to a BlockFarmEditorPropertyEditor
Use a BlockFarmEditorPropertyEditor to specify the property editor's config on the backend.
````csharp
public class AdvancedTextProperties : IBuilderProperties
{
    [BlockFarmEditorPropertyEditor("Umbraco.TextBox", "Title")]
    [BlockFarmEditorPropertyEditorConfig(typeof(TextBoxConfig))]
    public string? Title { get; set; }
}

public class TextBoxConfig : IBlockFarmEditorConfig
{
    public IEnumerable<BlockFarmEditorConfigItem> GetItems()
    {
        return new[]
        {
            new BlockFarmEditorConfigItem { Alias = "maxChars", Value = 100 },
            new BlockFarmEditorConfigItem { Alias = "minChars", Value = 5 }
        };
    }
}
````

### 3. Render Blocks in Templates
Use the ```<block-area>``` tag helper to render blocks in your templates:
````html
<div class="main-content">
    <block-area identifier="main-content"></block-area>
</div>

<div class="sidebar">
    <block-area identifier="sidebar-widgets"></block-area>
</div>

<section class="footer">
    <block-area identifier="footer-blocks"></block-area>
</section>
````
## Key Features
* **Assembly-based Registration:** Register blocks and containers using simple assembly attributes
* **Flexible Property Editors:** Use existing Umbraco data types or property editors
* **Custom Configuration:** Create reusable configuration classes for property editors
* **Template Integration:** Simple tag helper syntax for rendering block areas
* **View Components Support:** Use either Razor views or View Components for rendering
* **Container Support:** Create nested block structures with container blocks
