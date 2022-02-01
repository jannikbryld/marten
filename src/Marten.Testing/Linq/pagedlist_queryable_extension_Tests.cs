﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Marten.Exceptions;
using Marten.Linq;
using Marten.Pagination;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Linq
{
    public class PaginationTestDocument
    {
        public string Id { get; set; }
    }

    public class ToPagedListData<T> : IEnumerable<object[]>
    {
        private static readonly Func<IQueryable<T>, int, int, Task<IPagedList<T>>> ToPagedListAsync
            = (query, pageNumber, pageSize) => query.ToPagedListAsync(pageNumber, pageSize);

        private static readonly Func<IQueryable<T>, int, int, Task<IPagedList<T>>> ToPagedListSync
            = (query, pageNumber, pageSize) => Task.FromResult(query.ToPagedList(pageNumber, pageSize));

        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object []{ ToPagedListAsync };
            yield return new object[] { ToPagedListSync };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class pagedlist_queryable_extension_Tests : IntegrationContext
    {
        public pagedlist_queryable_extension_Tests(DefaultStoreFixture fixture) : base(fixture)
        {
        }

        protected override async Task fixtureSetup()
        {
            var targets = Target.GenerateRandomData(100).ToArray();

            await theStore.BulkInsertDocumentsAsync(targets);
        }

        private void BuildUpDocumentWithZeroRecords()
        {
            var doc = new PaginationTestDocument();
            doc.Id = "test";

            theSession.Store(doc);
            theSession.SaveChanges();

            theSession.Delete<PaginationTestDocument>(doc);
            theSession.SaveChanges();
        }

        [Fact]
        public void can_return_paged_result()
        {
            #region sample_to_paged_list
            var pageNumber = 2;
            var pageSize = 10;

            var pagedList = theSession.Query<Target>().ToPagedList(pageNumber, pageSize);

            // paged list also provides a list of helper properties to deal with pagination aspects
            var totalItems = pagedList.TotalItemCount; // get total number records
            var pageCount = pagedList.PageCount; // get number of pages
            var isFirstPage = pagedList.IsFirstPage; // check if current page is first page
            var isLastPages = pagedList.IsLastPage; // check if current page is last page
            var hasNextPage = pagedList.HasNextPage; // check if there is next page
            var hasPrevPage = pagedList.HasPreviousPage; // check if there is previous page
            var firstItemOnPage = pagedList.FirstItemOnPage; // one-based index of first item in current page
            var lastItemOnPage = pagedList.LastItemOnPage; // one-based index of last item in current page
            #endregion

            pagedList.Count.ShouldBe(pageSize);

        }

        [Fact]
        public async Task can_return_paged_result_async()
        {
            #region sample_to_paged_list_async
            var pageNumber = 2;
            var pageSize = 10;

            var pagedList = await theSession.Query<Target>().ToPagedListAsync(pageNumber, pageSize);
            #endregion

            pagedList.Count.ShouldBe(pageSize);
        }
        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task invalid_pagenumber_should_throw_exception(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            // invalid page number
            var pageNumber = 0;

            var pageSize = 10;

            var ex =
                await Exception<ArgumentOutOfRangeException>.ShouldBeThrownByAsync(
                    async () => await toPagedList(theSession.Query<Target>(), pageNumber, pageSize));
            SpecificationExtensions.ShouldContain(ex.Message, "pageNumber = 0. PageNumber cannot be below 1.");
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task invalid_pagesize_should_throw_exception(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            // invalid page size
            var pageSize = 0;

            var ex =
                await Exception<ArgumentOutOfRangeException>.ShouldBeThrownByAsync(
                    async () =>  await toPagedList(theSession.Query<Target>(), pageNumber, pageSize));
            SpecificationExtensions.ShouldContain(ex.Message, $"pageSize = 0. PageSize cannot be below 1.");
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_computed_pagecount(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            // page number ouside the page range, page range is between 1 and 10 for the sample
            var pageNumber = 1;

            var pageSize = 10;

            var expectedPageCount = theSession.Query<Target>().Count()/pageSize;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.PageCount.ShouldBe(expectedPageCount);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_total_items_count(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            var pageSize = 10;

            var expectedTotalItemsCount = theSession.Query<Target>().Count();

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.TotalItemCount.ShouldBe(expectedTotalItemsCount);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_has_previous_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 2;

            var pageSize = 10;

            var expectedHasPreviousPage = true;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.HasPreviousPage.ShouldBe(expectedHasPreviousPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_has_no_previous_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            var pageSize = 10;

            var expectedHasPreviousPage = false;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.HasPreviousPage.ShouldBe(expectedHasPreviousPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_has_next_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            var pageSize = 10;

            var expectedHasNextPage = true;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.HasNextPage.ShouldBe(expectedHasNextPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_has_no_next_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 10;

            var pageSize = 10;

            var expectedHasNextPage = false;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.HasNextPage.ShouldBe(expectedHasNextPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_is_first_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            var pageSize = 10;

            var expectedIsFirstPage = true;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.IsFirstPage.ShouldBe(expectedIsFirstPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_is_not_first_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 2;

            var pageSize = 10;

            var expectedIsFirstPage = false;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.IsFirstPage.ShouldBe(expectedIsFirstPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_is_last_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 10;

            var pageSize = 10;

            var expectedIsLastPage = true;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.IsLastPage.ShouldBe(expectedIsLastPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_is_not_last_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 1;

            var pageSize = 10;

            var expectedIsLastPage = false;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.IsLastPage.ShouldBe(expectedIsLastPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_first_item_on_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 2;

            var pageSize = 10;

            var expectedFirstItemOnPage = 11;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.FirstItemOnPage.ShouldBe(expectedFirstItemOnPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_last_item_on_page(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 2;

            var pageSize = 10;

            var expectedLastItemOnPage = 20;

            var pagedList = await toPagedList(theSession.Query<Target>(), pageNumber, pageSize);
            pagedList.LastItemOnPage.ShouldBe(expectedLastItemOnPage);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<PaginationTestDocument>))]
        public async Task zero_records_document_should_return_pagedlist_gracefully(Func<IQueryable<PaginationTestDocument>, int, int, Task<IPagedList<PaginationTestDocument>>> toPagedList)
        {
            BuildUpDocumentWithZeroRecords();

            var pageNumber = 1;

            var pageSize = 10;

            var pagedList = await toPagedList(theSession.Query<PaginationTestDocument>(), pageNumber, pageSize);
            pagedList.TotalItemCount.ShouldBe(0);
            pagedList.PageCount.ShouldBe(0);
            pagedList.IsFirstPage.ShouldBe(false);
            pagedList.IsLastPage.ShouldBe(false);
            pagedList.HasPreviousPage.ShouldBe(false);
            pagedList.HasNextPage.ShouldBe(false);
            pagedList.FirstItemOnPage.ShouldBe(0);
            pagedList.LastItemOnPage.ShouldBe(0);
            pagedList.PageNumber.ShouldBe(pageNumber);
            pagedList.PageSize.ShouldBe(pageSize);
        }

        [Theory]
        [ClassData(typeof(ToPagedListData<Target>))]
        public async Task check_query_with_where_clause_followed_by_to_pagedlist(Func<IQueryable<Target>, int, int, Task<IPagedList<Target>>> toPagedList)
        {
            var pageNumber = 2;
            var pageSize = 10;

            var pagedList = theSession.Query<Target>().Where(x=>x.Flag).ToPagedList(pageNumber, pageSize);
        }

        [Fact]
        public void try_to_use_in_compiled_query()
        {
            Exception<BadLinqExpressionException>.ShouldBeThrownBy(() =>
            {
                var data = theSession.Query(new TargetPage(1, 10));
            });
        }

        public class TargetPage: ICompiledQuery<Target, IPagedList<Target>>
        {
            public int Page { get; }
            public int PageSize { get; }

            public TargetPage(int page, int pageSize)
            {
                Page = page;
                PageSize = pageSize;
            }

            public Expression<Func<IMartenQueryable<Target>, IPagedList<Target>>> QueryIs()
            {
                return q => q.OrderBy(x => x.Number).ToPagedList(Page, PageSize);
            }
        }
    }
}