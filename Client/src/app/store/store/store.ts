import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ProductService } from '../services/product.service';
import { Product } from '../models/Product';
import { Brand } from '../models/Brand';
import { Type } from '../models/Type';
import { BasketService } from '../services/basket.service';
import { BasketItem, Basket } from '../models/Basket';

@Component({
  selector: 'app-store',
  imports: [CommonModule, RouterModule],
  standalone: true,
  templateUrl: './store.html',
  styleUrl: './store.scss',
})
export class Store implements OnInit {
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);
  private basketService = inject(BasketService);

  // Signals
  products = signal<Product[]>([]);
  totalCount = signal(0);
  brands = signal<Brand[]>([]);
  types = signal<Type[]>([]);
  searchTerm = signal<string>('');
  searchMode = signal<string>('catalog');

  // Filters (store IDs instead of names)
  selectedBrandId = signal<string | null>(null);
  selectedTypeId = signal<string | null>(null);
  sortOption = signal('default');

  // Pagination
  pageSize = 10;
  currentPage = signal(1);

  ngOnInit(): void {
    this.loadProducts();
    this.loadBrands();
    this.loadTypes();

    // watch for search query param
    this.route.queryParams.subscribe((params) => {
      this.searchTerm.set(params['search'] || '');
      this.selectedTypeId.set(params['typeId'] || null);
      this.searchMode.set(params['mode'] || 'catalog'); //semantic/hybrid/catalog
      this.currentPage.set(1); // reset pagination on new search
      this.loadProducts();
    });
  }

  loadProducts() {
    const query = this.searchTerm();
    if (!query || this.searchMode() === 'catalog') {
      this.productService
        .getAllProducts(
          this.currentPage(),
          this.pageSize,
          this.selectedBrandId(),
          this.selectedTypeId(),
          this.sortOption(),
          query,
        )
        .subscribe((res) => {
          this.products.set(res.data);
          this.totalCount.set(res.count);
        });
    } else if(this.searchMode() === 'semantic'){
      //semantic search
      this.productService.searchSemantic(query).subscribe((res)=>{
        let filtered = res;

        if(this.selectedBrandId()){
          filtered = filtered.filter(p => p.brand.id === this.selectedBrandId());
        }
        if(this.selectedTypeId()){
          filtered = filtered.filter(p => p.type.id === this.selectedTypeId());
        }
        this.products.set(filtered);
        this.totalCount.set(filtered.length);
      });
    } else if(this.searchMode() === 'hybrid'){
      //hybrid search
      this.productService.searchHybrid(query).subscribe((res)=>{
        let filtered = res;

        if(this.selectedBrandId()){
          filtered = filtered.filter(p => p.brand.id === this.selectedBrandId());
        }
        if(this.selectedTypeId()){
          filtered = filtered.filter(p => p.type.id === this.selectedTypeId());
        }
        this.products.set(filtered);
        this.totalCount.set(filtered.length);
      });
    }
  }

  loadBrands() {
    this.productService.getAllBrands().subscribe((res) => this.brands.set(res));
  }

  loadTypes() {
    this.productService.getAllTypes().subscribe((res) => this.types.set(res));
  }

  // Apply filters
  applyFilters() {
    this.currentPage.set(1); // reset pagination
    this.loadProducts(); // backend does filtering
  }

  // Add to Cart
  addToCart(p: Product) {
    const newItem: BasketItem = {
      productId: p.id,
      productName: p.name,
      price: p.price,
      quantity: 1,
      imageFile: p.imageFile,
    };

    this.basketService.getBasket('rahul.sahay').subscribe((current) => {
      let items = [...current.items];
      const existing = items.find((i) => i.productId === p.id);
      if (existing) {
        existing.quantity += 1;
      } else {
        items.push(newItem);
      }

      const basket: Basket = {
        userName: 'rahul.sahay',
        items,
        totalPrice: items.reduce((sum, i) => sum + i.price * i.quantity, 0),
      };

      this.basketService.updateBasket(basket).subscribe({
        next: (res) => this.basketService.setBasket(res),
        error: (err) => console.log('Error adding to basket:', err),
      });
    });
  }

  // Reset filters
  resetFilters() {
    this.searchTerm.set('');
    this.selectedBrandId.set(null);
    this.selectedTypeId.set(null);
    this.sortOption.set('default');
    this.currentPage.set(1);
    this.loadProducts();
  }

  totalPages = () => Math.ceil(this.totalCount() / this.pageSize);

  // Pagination controls
  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadProducts();
    }
  }
}
