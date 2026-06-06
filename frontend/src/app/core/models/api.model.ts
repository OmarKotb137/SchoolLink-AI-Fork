export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}

export interface OperationResult<T> {
  isSuccess: boolean;
  message?: string;
  data: T;
  errors?: string[];
}
