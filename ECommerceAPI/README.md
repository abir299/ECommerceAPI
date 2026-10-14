# ECommerceAPI

A feature-rich E-Commerce REST API built with ASP.NET Core Web API, Entity Framework Core, and SQL Server. This project demonstrates secure authentication, role-based authorization, product catalog management, shopping cart, checkout, inventory control, and order lifecycle management.

## Features

- **Authentication: User registration, login, password hashing, and JWT authentication.
- **Role-Based Authorization: Protected endpoints for Admin, Seller, and Customer roles.
- **Categories & Brands: Create, retrieve, update, and soft-delete categories and brands.
- **Product Management: Product listing, details, search, filtering, sorting, pagination, and soft deletion.
- **Product Images: Manage product images and primary image selection.
- **Product Variants: Manage size, color, additional price, and variant-level stock.
- **Shopping Cart:View cart, add products, update quantities, and remove items.
- **Addresses: Manage customer shipping addresses.
- **Wishlist: Add, view, and remove wishlist products.
- **Reviews: Product ratings and review management.
- **Checkout & Orders: Create orders from cart items, calculate totals, validate inventory, and save order records.
- **Inventory Control: Atomically deduct stock during checkout and restore stock when an eligible order is cancelled.
- **Order Lifecycle: Validate status transitions and maintain order status history.
- **Order Cancellation: Customers can cancel eligible orders; repeated or invalid cancellation is prevented.
- **Payments: Cash on Delivery payment support.
- **Notifications: View notifications, check unread count, and mark notifications as read.
- **DTO Responses: Return structured order and order-item data without exposing unnecessary entity navigation properties.

## Technology Stack

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- REST APIs
- Postman
- Git & GitHub

## Database Setup

- Database: `ECommerceDb`
- ORM: Entity Framework Core
- Connection string key: `DefaultConnection`

Configure your SQL Server connection string in `appsettings.json`. Never commit database passwords, JWT secrets, or other credentials.

## Getting Started

1. Clone this repository.
2. Open `ECommerceAPI.sln` or the project file in Visual Studio.
3. Configure the SQL Server connection string.
4. Restore NuGet packages.
5. Create or update the database using the migration workflow configured for this project.
6. Run the API from Visual Studio.
7. Use Postman to register or log in and obtain a JWT token.
8. Send the token as a Bearer token for protected endpoints.

## API Endpoints

| Feature | Method | Endpoint |
|---|---|---|
| Register | POST | `/api/Account/register` |
| Login | POST | `/api/Account/login` |
| Categories | GET | `/api/Categories` |
| Brands | GET | `/api/Brands` |
| Products | GET | `/api/Product` |
| Cart | GET | `/api/Cart` |
| Add to Cart | POST | `/api/Cart/items` |
| Addresses | GET | `/api/Addresses` |
| Create Address | POST | `/api/Addresses` |
| Wishlist | GET | `/api/Wishlists` |
| Product Reviews | GET | `/api/Reviews/product/{productId}` |
| Product Images | GET | `/api/ProductImages/product/{productId}` |
| Product Variants | GET | `/api/ProductVariants/product/{productId}` |
| Checkout | POST | `/api/Orders/checkout` |
| My Orders | GET | `/api/Orders/my-orders` |
| Get Order | GET | `/api/Orders/{id}` |
| Update Order Status | PUT | `/api/Orders/{id}/status` |
| Cancel Order | PUT | `/api/Orders/{id}/cancel` |
| Order Status History | GET | `/api/Orders/{id}/status-history` |
| Notifications | GET | `/api/Notifications` |
| Unread Notification Count | GET | `/api/Notifications/unread-count` |

Endpoints require authentication and the appropriate role. Order status history and administrative status updates require the Admin role. Customers can access only their own orders.

## Order Status Flow

- `Pending` → `Confirmed` or `Cancelled`
- `Confirmed` → `Processing` or `Cancelled`
- `Processing` → `Shipped` or `Cancelled`
- `Shipped` → `Delivered`
- `Delivered` and `Cancelled` are terminal states.

Eligible customer cancellations restore the associated product-variant stock and create a status-history record.

## Testing

Postman tests performed include:

- Customer checkout and order creation.
- Cart validation and stock deduction.
- Order cancellation and stock restoration.
- Order status history retrieval.
- Successful Admin status updates.
- Rejection of invalid status transitions.
- Authorization checks for Admin-only endpoints.
- DTO-based order list and single-order responses.

## Future Improvements

- Automated unit and integration tests
- Online payment gateway integration
- Automated order notifications
- Product image upload and storage integration
- Docker deployment and CI/CD

## Author

**MD. Abir Ahmed**  
B.Sc. in Computer Science and Engineering
