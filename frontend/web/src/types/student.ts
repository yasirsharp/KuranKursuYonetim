export interface IStudent {
  id: string;
  tenantId: string;
  groupId: string | null;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  currentPage: number;
  completedPages: number;
  status: string;
  photoUrl: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ICreateStudent {
  firstName: string;
  lastName: string;
  identificationNumber: string;
  groupId?: string;
  currentPage?: number;
  notes?: string;
}

export interface IUpdateStudent {
  id: string;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  groupId?: string;
  currentPage?: number;
  completedPages?: number;
  status?: string;
  notes?: string;
}
