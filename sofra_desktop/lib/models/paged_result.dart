class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
  });

  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;

  int get totalPages => pageSize == 0 ? 0 : (totalCount / pageSize).ceil();

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromJsonT,
  ) =>
      PagedResult(
        items: (json['items'] as List<dynamic>)
            .map((e) => fromJsonT(e as Map<String, dynamic>))
            .toList(),
        totalCount: json['totalCount'] as int,
        page: json['page'] as int,
        pageSize: json['pageSize'] as int,
      );
}
