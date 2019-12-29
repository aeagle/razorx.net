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

## Why?

I've been writing React applications using JSX for a while now and appreciate the composability of creating reusable components. With ASP.NET / Razor I have come to find the current methods of creating reusable presentational components lacking:

- Partials

Can't wrap other content unless you specifically create start and end partials.

- HTMLHelpers / TagHelpers

You have to write C# code which renders markup. Not to mention you can't use tag helpers in ASP.NET MVC (non-core).

- @helper

Doesn't deal with composability of multiple helpers in a nice way.

## How it works

In a nutshell:

- A tag formed with the name `component-xxx` will be processed into a `@Html.Partial("xxx")`
- If the tag is not self closing there will be 2 `@Html.Partial()` references for the start and end
- All tag attributes are added to a dynamic object and passed as the model to the partial
- If a partial contains `@Model.children` it will automatically split by the view engine to form the start and end portions of markup

## Complex example

Apart from the special `commponent-` tag the rest of the `cshtml` file is rendered by the default Razor view engine allowing you to use expressions and constructs as you normally would:

```html
<component-boxout classname="my-boxout" type="@BoxoutType.Bordered">

    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Pellentesque
    vitae purus id urna ornare convallis. Mauris ac cursus tortor. Phasellus
    pharetra lacus a nunc eleifend aliquam.

    <component-cardcontainer size="@CardSize.Third">

        @foreach (var idx in Enumerable.Range(1, 3))
        {
            <text>
                <component-card>
                    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Pellentesque
                    vitae purus id urna ornare convallis. Mauris ac cursus tortor. Phasellus
                    pharetra lacus a nunc eleifend aliquam.
                </component-card>
            </text>
        }

    </component-cardcontainer>

</component-boxout>
```
