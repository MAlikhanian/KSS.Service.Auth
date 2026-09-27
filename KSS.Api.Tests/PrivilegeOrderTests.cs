using KSS.Service.IService;
using Xunit;

namespace KSS.Api.Tests
{
    public class PrivilegeOrderTests
    {
        private static IReadOnlySet<string> Set(params string[] codes) => new HashSet<string>(codes, StringComparer.Ordinal);

        [Fact]
        public void Identical_sets_are_equal()
            => Assert.Equal(PrivilegeRelation.Equal, PrivilegeOrder.Compare(Set("a", "b"), Set("b", "a")));

        [Fact]
        public void Strict_superset_dominates()
            => Assert.Equal(PrivilegeRelation.Dominates, PrivilegeOrder.Compare(Set("a", "b", "c"), Set("a", "b")));

        [Fact]
        public void Strict_subset_is_dominated()
            => Assert.Equal(PrivilegeRelation.DominatedBy, PrivilegeOrder.Compare(Set("a"), Set("a", "b")));

        [Fact]
        public void Overlapping_sets_neither_containing_the_other_are_incomparable()
            => Assert.Equal(PrivilegeRelation.Incomparable, PrivilegeOrder.Compare(Set("a", "b"), Set("b", "c")));

        [Fact]
        public void Disjoint_non_empty_sets_are_incomparable()
            => Assert.Equal(PrivilegeRelation.Incomparable, PrivilegeOrder.Compare(Set("a"), Set("z")));

        [Fact]
        public void A_set_covers_itself_and_every_subset_including_empty()
        {
            Assert.True(PrivilegeOrder.Covers(Set("a", "b"), Set("a", "b")));
            Assert.True(PrivilegeOrder.Covers(Set("a", "b"), Set("a")));
            Assert.True(PrivilegeOrder.Covers(Set("a", "b"), Set()));
            Assert.False(PrivilegeOrder.Covers(Set("a"), Set("a", "b")));
        }

        [Fact]
        public void Codes_are_compared_ordinally_so_case_differs()
            => Assert.Equal(PrivilegeRelation.Incomparable, PrivilegeOrder.Compare(Set("Person.Security.Modify"), Set("person.security.modify")));

        // Mirrors the shape of the real catalog: a global role holding most permissions
        // but deliberately not a module's, and a module role holding only that module's.
        [Fact]
        public void Global_set_without_a_module_and_that_module_set_are_both_maximal()
        {
            var global = Set("Person.Security.Modify", "Company.Information.Modify", "Developer.Access");
            var moduleOnly = Set("Dms.Project.Control");
            var lower = Set("Person.Security.Modify");

            var maximal = PrivilegeOrder.MaximalSets(new[] { global, moduleOnly, lower });

            Assert.Equal(2, maximal.Count);
            Assert.Contains(maximal, s => s.SetEquals(global));
            Assert.Contains(maximal, s => s.SetEquals(moduleOnly));
            Assert.DoesNotContain(maximal, s => s.SetEquals(lower));
            Assert.Equal(PrivilegeRelation.Incomparable, PrivilegeOrder.Compare(global, moduleOnly));
        }

        [Fact]
        public void Maximal_sets_collapse_duplicates_and_ignore_the_empty_set()
        {
            var maximal = PrivilegeOrder.MaximalSets(new[] { Set("a", "b"), Set("b", "a"), Set(), Set("a") });

            Assert.Single(maximal);
            Assert.True(maximal[0].SetEquals(Set("a", "b")));
        }

        [Fact]
        public void No_sets_yield_no_maximal_sets()
            => Assert.Empty(PrivilegeOrder.MaximalSets(Array.Empty<IReadOnlySet<string>>()));
    }
}
