export enum LibraryItemType {
  Book = 1,
  File = 2,
  Video = 3,
  Link = 4,
  Note = 5
}

export interface LibraryItemDto {
  id: number;
  title: string;
  description?: string;
  itemType: LibraryItemType;
  fileUrl?: string;
  subjectId?: number;
  subjectName?: string;
  gradeLevelId?: number;
  gradeLevelName?: string;
  academicYearId?: number;
  uploadedById: number;
  uploadedByName: string;
  isActive: boolean;
  fileSizeBytes?: number;
  createdAt: string;
}

export interface PaginationFilter {
  page: number;
  pageSize: number;
}

export interface GetLibraryFilter extends PaginationFilter {
  subjectId?: number | null;
  gradeLevelId?: number | null;
  academicYearId?: number | null;
  itemType?: LibraryItemType | null;
  searchTerm?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface OperationResult<T> {
  isSuccess: boolean;
  message?: string;
  data: T;
  errors?: string[];
}
