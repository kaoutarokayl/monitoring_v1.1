export interface TicketSearchCriteria {
  groupId?: number;
  businessId?: number;
  branchId?: number;
  atmName?: string;
  createdAfter?: string;
  createdBefore?: string;
  ticketTypeId?: number;
  errorCodeTypeId?: number;
  owner?: string;
  ticketStatus?: 'All' | 'Open/Dispatched' | 'Closed';
  dispatchedTo?: string;
  slaStatus?: 'No Filter' | 'No Ticket SLAs' | 'Has any open SLAs' | 'Has any due in <X hours' | 'Has open exceeded SLAs' | 'All SLAs are closed';
  slaHours?: number;
  extraDataField?: string;
  extraDataValue?: string;
  ticketId?: number;
}

export interface TicketSearchResult {
  ticketId: number;
  ticketType: string;
  status: string;
  atmName: string;
  businessName: string;
  branchName: string;
  groupName: string;
  dispatchedTo: string;
  owner: string;
  errorCode: string;
  errorText: string;
  created: string;
  lastChangeDate: string;
  closedDate?: string;
  duration: string;
  slaSummary: string;
}

export interface TicketTypeLookupDto {
  ticketTypeId: number;
  typeName: string;
}

export interface ErrorCodeLookupDto {
  errorCodeTypeId: number;
  errorCode: string;
  errorText?: string;
}

export interface GroupDto {
  groupId: number;
  groupName: string;
}

export interface BranchDto {
  branchId: number;
  branchName: string;
  displayId: string;
}

export interface BusinessDto {
  businessId: number;
  businessName: string;
  displayId: string;
}

export interface SlaStatusOption {
  value: TicketSearchCriteria['slaStatus'];
  label: string;
}
