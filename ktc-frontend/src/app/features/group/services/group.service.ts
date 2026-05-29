import { Group, GroupDetails, CreateGroupRequest, UpdateGroupRequest } from '../models/group.models';
export * from '../models/group.models';
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { ClientAtm } from '../../atm/services/atm.service';

// Re-export ClientAtm for convenience
@Injectable({
  providedIn: 'root'
})
export class GroupService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5239/api/group';
  private readonly groupModifiedSubject = new Subject<number>();

  groupModified$ = this.groupModifiedSubject.asObservable();

  notifyGroupModified(groupId: number): void {
    this.groupModifiedSubject.next(groupId);
  }

  // -- R�cup�rer tous les groupes --
  getAllGroups(): Observable<Group[]> {
    return this.http.get<Group[]>(this.apiUrl);
  }

  // -- R�cup�rer les d�tails d'un groupe avec ses clients --
  getGroupDetails(groupId: number): Observable<GroupDetails> {
    return this.http.get<GroupDetails>(`${this.apiUrl}/${groupId}`);
  }

  // -- R�cup�rer les clients d'un groupe --
  getGroupClients(groupId: number): Observable<ClientAtm[]> {
    return this.http.get<ClientAtm[]>(`${this.apiUrl}/${groupId}/clients`);
  }

  // -- Cr�er un nouveau groupe --
  createGroup(request: CreateGroupRequest): Observable<any> {
    return this.http.post(this.apiUrl, request);
  }

  // -- Mettre � jour un groupe --
  updateGroup(request: UpdateGroupRequest): Observable<any> {
    return this.http.put(this.apiUrl, request);
  }

  // -- Ajouter un client � un groupe --
  addClientToGroup(groupId: number, clientId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${groupId}/add-client/${clientId}`, {})
      .pipe(tap({
        next: () => this.notifyGroupModified(groupId)
      }));
  }

  // -- Retirer un client d'un groupe --
  removeClientFromGroup(groupId: number, clientId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${groupId}/remove-client/${clientId}`)
      .pipe(tap({
        next: () => this.notifyGroupModified(groupId)
      }));
  }

  // -- Supprimer un groupe --
  deleteGroup(groupId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${groupId}`);
  }
}



