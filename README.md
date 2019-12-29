# razorx.net

Experimental ASP.NET Razor view engine supplemented with JSX style component composability allowing you to do this:

### views/home/index.cshtml

```html
<component-tabcontainer>
    <component-tab active="@true">
        Tab 1
    </component-tab>
    <component-tab active="@false">
        Tab 2
    </component-tab>
    <component-tab active="@false">
        Tab 3
    </component-tab>
</component-tabcontainer>
```

When you create these partials:

### /views/shared/tabcontainer.cshtml

```html
<div class="tabcontainer">
    @Model.children
</div>
```

### /view/shared/tab.cshtml
```html
<div class="tab@(Model.active ? " active" : "")">
    <a>
        @Model.children
    </a>
</div>
```

## How it works

- A tag formed with the name `component-xxx` will be processed into a @Html.Partial("xxx")
- If the tag is not self closing there will be 2 @Html.Partial() references for the start and end
- All tag attributes are added to a dynamic object and passed as the model to the partial
- If a partial contains @Model.children it will automatically split by the view engine to form the start and end portions of marked
