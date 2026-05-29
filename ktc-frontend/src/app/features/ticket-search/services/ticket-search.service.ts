import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TicketSearchCriteria, TicketSearchResult, TicketTypeLookupDto, ErrorCodeLookupDto, GroupDto, BusinessDto, BranchDto } from '../models/ticket-search.models';

@Injectable({ providedIn: 'root' })
export class TicketSearchService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5239/api/ticket';
  private readonly atmApiUrl = 'http://localhost:5239/api/atm';
  private readonly groupApiUrl = 'http://localhost:5239/api/group';

  getGroups(): Observable<GroupDto[]> {
    return this.http.get<GroupDto[]>(this.groupApiUrl);
  }

  getBusinesses(): Observable<BusinessDto[]> {
    return this.http.get<BusinessDto[]>(`${this.atmApiUrl}/businesses`);
  }

  getBranches(): Observable<BranchDto[]> {
    return this.http.get<BranchDto[]>(`${this.atmApiUrl}/branches`);
  }

  getTicketTypes(): Observable<TicketTypeLookupDto[]> {
    return this.http.get<TicketTypeLookupDto[]>(`${this.apiUrl}/types`);
  }

  getErrorCodes(): Observable<ErrorCodeLookupDto[]> {
    return this.http.get<ErrorCodeLookupDto[]>(`${this.apiUrl}/error-codes`);
  }

  searchTickets(criteria: TicketSearchCriteria): Observable<TicketSearchResult[]> {
    return this.http.post<TicketSearchResult[]>(`${this.apiUrl}/search`, criteria);
  }
}
