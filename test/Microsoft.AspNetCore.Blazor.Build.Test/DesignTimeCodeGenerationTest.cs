// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class DesignTimeCodeGenerationTest : RazorBaselineIntegrationTestBase
    {
        internal override bool DesignTime => true;

        internal override bool UseTwoPhaseCompilation => true;
        
        [Fact]
        public void ChildComponent_WithParameters()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class SomeType
    {
    }

    public class MyComponent : BlazorComponent
    {
        [Parameter] int IntProperty { get; set; }
        [Parameter] bool BoolProperty { get; set; }
        [Parameter] string StringProperty { get; set; }
        [Parameter] SomeType ObjectProperty { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent 
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ComponentParameter_TypeMismatch_ReportsDiagnostic()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class CoolnessMeter : BlazorComponent
    {
        [Parameter] private int Coolness { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<CoolnessMeter Coolness=""@(""very-cool"")"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS1503", d.Id));
        }

        [Fact]
        public void ChildComponent_WithExplicitStringParameter()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        string StringProperty { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent StringProperty=""@(42.ToString())"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithNonPropertyAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent some-attribute=""foo"" another-attribute=""@(43.ToString())""/>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithLambdaEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        Action<UIEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent OnClick=""@((e) => { Increment(); })""/>

@functions {
    private int counter;
    private void Increment() {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        // Regression test for #954 - we need to allow arbitrary event handler
        // attributes with weak typing.
        [Fact]
        public void ChildComponent_WithWeaklyTypeEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class DynamicElement : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<DynamicElement onclick=""@OnClick"" />

@functions {
    private Action<UIMouseEventArgs> OnClick { get; set; }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithExplicitEventHandler()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        Action<UIEventArgs> OnClick { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent OnClick=""@Increment""/>

@functions {
    private int counter;
    private void Increment(UIEventArgs e) {
        counter++;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        string MyAttr { get; set; }

        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent MyAttr=""abc"">Some text<some-child a='1'>Nested text</some-child></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void ChildComponent_WithElementOnlyChildContent()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        RenderFragment ChildContent { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent><child>hello</child></MyComponent>");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithString()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""foo"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithLambdaDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@(x => { })"" />");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void EventHandler_OnElement_WithDelegate()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input onclick=""@OnClick"" />
@functions {
    void OnClick(UIMouseEventArgs e) {
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_Action_MethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""@OnClick"" />
@functions {
    Task OnClick() 
    {
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_ActionEventArgs_MethodGroup()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""@OnClick"" />
@functions {
    Task OnClick(UIMouseEventArgs e) 
    {
        return Task.CompletedTask;
    }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_Action_Lambda()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""async (e) => await Task.Delay(10)"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void AsyncEventHandler_OnElement_ActionEventArgs_Lambda()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@using System.Threading.Tasks
<input onclick=""async (e) => await Task.Delay(10)"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/597
        public void Regression_597()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class Counter : BlazorComponent
    {
        public int Count { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<Counter bind-v=""y"" />
@functions {
    string y = null;
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Regression_609()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class User : BlazorComponent
    {
        public string Name { get; set; }
        public Action<string> NameChanged { get; set; }
        public bool IsActive { get; set; }
        public Action<bool> IsActiveChanged { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<User bind-Name=""@UserName"" bind-IsActive=""@UserIsActive"" />

@functions {
    public string UserName { get; set; }
    public bool UserIsActive { get; set; }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/772
        public void Regression_772()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class SurveyPrompt : BlazorComponent
    {
        [Parameter] private string Title { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            // This has some errors
            Assert.Collection(
                generated.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("RZ1034", d.Id),
                d => Assert.Equal("RZ1035", d.Id));
        }

        [Fact] // https://github.com/aspnet/Blazor/issues/773
        public void Regression_773()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class SurveyPrompt : BlazorComponent
    {
        [Parameter] private string Title { get; set; }
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@page ""/""

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title=""<div>Test!</div>"" />
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Regression_784()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<p onmouseover=""@OnComponentHover"" style=""background: @ParentBgColor;"" />
@functions {
    public string ParentBgColor { get; set; } = ""#FFFFFF"";

    public void OnComponentHover(UIMouseEventArgs e)
    {
    }
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValue_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_TypeChecked_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> ValueChanged { get; set; }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""42"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);

            var assembly = CompileToAssembly(generated, throwOnFailure: false);
            // This has some errors
            Assert.Collection(
                assembly.Diagnostics.OrderBy(d => d.Id),
                d => Assert.Equal("CS0029", d.Id),
                d => Assert.Equal("CS1503", d.Id));
        }

        [Fact]
        public void BindToComponent_SpecifiesValue_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndChangeEvent_WithMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
        [Parameter]
        int Value { get; set; }

        [Parameter]
        Action<int> OnChanged { get; set; }
    }
}"));
            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value-OnChanged=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToComponent_SpecifiesValueAndChangeEvent_WithoutMatchingProperties()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent, IComponent
    {
        void IComponent.SetParameters(ParameterCollection parameters)
        {
        }
    }
}"));

            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent bind-Value-OnChanged=""ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElement_WritesAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    [BindElement(""div"", null, ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<div bind=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementWithSuffix_WritesAttributes()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    [BindElement(""div"", ""value"", ""myvalue"", ""myevent"")]
    public static class BindAttributes
    {
    }
}"));
            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<div bind-value=""@ParentValue"" />
@functions {
    public string ParentValue { get; set; } = ""hi"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputWithoutType_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input bind=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WithFormat_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@CurrentDate"" format-value=""MM/dd/yyyy""/>
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WithFormatFromProperty_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@CurrentDate"" format-value=""@Format""/>
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);

    public string Format { get; set; } = ""MM/dd/yyyy"";
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputText_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BuiltIn_BindToInputCheckbox_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""checkbox"" bind=""@Enabled"" />
@functions {
    public bool Enabled { get; set; }
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementFallback_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind-value-onchange=""@ParentValue"" />
@functions {
    public int ParentValue { get; set; } = 42;
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void BindToElementFallback_WithFormat_WritesAttributes()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<input type=""text"" bind-value-onchange=""@CurrentDate"" format-value=""MM/dd"" />
@functions {
    public DateTime CurrentDate { get; set; } = new DateTime(2018, 1, 1);
}");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Element_WithRef()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<elem attributebefore=""before"" ref=""myElem"" attributeafter=""after"">Hello</elem>

@functions {
    Microsoft.AspNetCore.Blazor.ElementRef myElem;

    void DoSomething() { myElem.GetHashCode(); } // Avoid 'assigned but not used' warning
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }

        [Fact]
        public void Component_WithRef()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Blazor.Components;

namespace Test
{
    public class MyComponent : BlazorComponent
    {
    }
}
"));

            // Act
            var generated = CompileToCSharp(@"
@addTagHelper *, TestAssembly
<MyComponent ParamBefore=""before"" ref=""myInstance"" ParamAfter=""after"" />

@functions {
    Test.MyComponent myInstance;

    void DoSomething() { myInstance.GetHashCode(); } // Avoid 'assigned but not used' warning
}
");

            // Assert
            AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
            AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
            CompileToAssembly(generated);
        }
    }
}
