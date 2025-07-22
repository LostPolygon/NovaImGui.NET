using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq.Extensions;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static SyntaxFactoryCached;
using static BindingsGenerationUtility;

public static class CommonModifiers {
    /// <summary>
    /// If all entity members have the same conditional define,
    /// combines them by wrapping the entire entity in that same condition.
    /// This avoids having an empty entity which has all its member defined out.
    /// </summary>
    // TODO: also wrap members with consecutive identical conditionals
    public static T WrapInSharedConditional<T>(
        T entity,
        IReadOnlyList<IIndependentEntity> members
    ) where T : IIndependentEntity {
        if (members.Count == 0)
            return entity;

        IReadOnlyList<Conditional> conditionalList = members[0].Conditionals;
        if (conditionalList.Count != 1)
            return entity;

        Conditional conditional = conditionalList[0];
        bool allMembersHaveSameSingleConditional =
            members
                .All(member =>
                    member.Conditionals.Count == 1 &&
                    member.Conditionals.First() == conditional
                );
        if (!allMembersHaveSameSingleConditional)
            return entity;

        entity.Conditionals = [..entity.Conditionals, conditional];
        members.ForEach(member => member.Conditionals = []);

        return entity;
    }

    public static T AddObsoleteAttributes<T>(
        T entity,
        IReadOnlyList<IIndependentEntity> members
    ) where T : IIndependentEntity {
        AddObsoleteAttributes(entity);
        members.ForEach(AddObsoleteAttributes);
        return entity;
    }

    private static void AddObsoleteAttributes<T>(T entity) where T : IIndependentEntity {
        Conditional obsoletingConditional =
            entity.Conditionals
                .FirstOrDefault(c =>
                    c.Condition == "ifndef" &&
                    c.Expression.Contains("IMGUI_DISABLE_OBSOLETE_")
                );
        if (String.IsNullOrEmpty(obsoletingConditional.Expression))
            return;

        AttributeSyntax obsoleteAttribute =
            SimpleAttribute(
                "Obsolete",
                LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(obsoletingConditional.Expression))
            );

        entity.Attributes = [
            ..entity.Attributes,
            obsoleteAttribute
        ];
    }
}
