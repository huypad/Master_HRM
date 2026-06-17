// menu.service.ts
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { QueryParamsModel } from '../../../components/HRM/_core/models/query-models/query-params.model';
import { HttpUtilsService } from '../../../components/HRM/_core/utils/http-utils.service';
import { QueryResultsModel } from '../../../components/HRM/_core/models/query-models/query-results.model';

@Injectable({
  providedIn: 'root',
})
export class MenuServices {
  constructor(private http: HttpClient, private httpUtils: HttpUtilsService) {}

  layMenuChucNang(): Observable<QueryResultsModel> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    return this.http.get<any>(`${environment.apiUrl}/menu/LayMenuChucNang`, { headers: httpHeaders });
  }
}
