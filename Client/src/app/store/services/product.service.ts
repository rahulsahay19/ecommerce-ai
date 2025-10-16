import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CatalogResponse } from '../models/CatalogResponse';
import { Brand } from '../models/Brand';
import { Type } from '../models/Type';
import { Product } from '../models/Product';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  // Gateway Url
  private baseUrl = 'http://localhost:8010/Catalog';
  //AI Service
  private aiUrl = 'https://localhost:7138';

  getAllProducts(
    page: number,
    size: number,
    brandId?: string | null,
    typeId?: string | null,
    sort?: string | null,
    search?: string | null,
  ): Observable<CatalogResponse> {
    let params: string[] = [`pageIndex=${page}`, `pageSize=${size}`];

    if (brandId) params.push(`BrandId=${brandId}`); 
    if (typeId) params.push(`TypeId=${typeId}`); 
    if (sort && sort !== 'default') params.push(`sort=${sort}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);

    return this.http.get<CatalogResponse>(`${this.baseUrl}/GetAllProducts?${params.join('&')}`);
  }
  getAllBrands(): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.baseUrl}/GetAllBrands`);
  }

  getAllTypes(): Observable<Type[]> {
    return this.http.get<Type[]>(`${this.baseUrl}/GetAllTypes`);
  }

  getProductById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  searchSemantic(query: string, topK: number = 10): Observable<Product[]>{
    return this.http.post<Product[]>(`${this.aiUrl}/search/vector`, {
      query,
      topK
    });
  }
  searchHybrid(query: string, topK: number = 10): Observable<Product[]>{
    return this.http.post<Product[]>(`${this.aiUrl}/search/hybrid`, {
      query,
      topK
    }); 
  }
}
