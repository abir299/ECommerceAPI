# ECommerceAPI

A feature-rich E-Commerce REST API built with ASP.NET Core Web API and Entity Framework Core. The project demonstrates authentication, role-based authorization, product management, shopping cart, checkout, order processing, and other essential e-commerce features.

## Features

- **Authentication:** User registration, login, password hashing, and JWT authentication.
- **Role-Based Authorization:** Protected endpoints for Admin, Seller, and Customer roles.
- **Categories & Brands:** Create, retrieve, update, and soft-delete categories and brands.
- **Product Management:** Product listing, details, search, filtering, sorting, pagination, and soft deletion.
- **Product Images:** Add, view, and delete product images, including primary image selection.
- **Product Variants:** Manage product size, color, additional price, and stock quantity.
- **Shopping Cart:** View cart, add products, update quantities, and remove items.
- **Addresses:** Manage customer shipping addresses.
- **Wishlist:** Add, view, and remove wishlist products.
- **Reviews:** Add, view, and delete product reviews with 1–5 ratings.
- **Checkout & Orders:** Place orders from the cart, calculate totals, validate stock, and create order records.
- **Payments:** Cash on Delivery payment records.
- **Order Management:** View orders and update order status with Admin authorization.
- **Notifications:** View notifications, check unread count, and mark notifications as read.
- **DTO Responses:** Structured order responses to avoid exposing entity navigation properties and circular references.

## Technology Stack

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- REST APIs
- Postman
- Git & GitHub

## Database

- Database: `ECommerceDb`
- ORM: Entity Framework Core
- Connection string key: `DefaultConnection`

Configure your SQL Server connection string in `appsettings.json` before running the application. Do not commit real database credentials or secrets.

## Getting Started

1. Clone the repository.
2. Open `ECommerceAPI.sln` or the project file in Visual Studio.
3. Configure the SQL Server connection string in `appsettings.json`.
4. Restore NuGet packages.
5. Apply Entity Framework Core migrations if the database has not been created:

   ```powershell
   Update-Database
   ```

6. Run the project.
7. Use Postman to test the API endpoints. Obtain a JWT token through the login endpoint and send it as a Bearer token for protected endpoints.

## API Endpoints

| Feature | Method | Endpoint |
|---|---|---|
| Register | POST | `/api/Account/register` |
| Login | POST | `/api/Account/login` |
| Categories | GET | `/api/Categories` |
| Brands | GET | `/api/Brands` |
| Products | GET | `/api/Product` |
| Cart | GET | `/api/Cart` |
| Addresses | GET | `/api/Address` |
| Wishlist | GET | `/api/Wishlists` |
| Product Reviews | GET | `/api/Reviews/product/{productId}` |
| Product Images | GET | `/api/ProductImages/product/{productId}` |
| Product Variants | GET | `/api/ProductVariants/product/{productId}` |
| Checkout | POST | `/api/Orders/checkout` |
| My Orders | GET | `/api/Orders/my-orders` |
| Notifications | GET | `/api/Notifications` |
| Unread Notification Count | GET | `/api/Notifications/unread-count` |

Protected endpoints require the appropriate authenticated user role.

## Testing

API endpoints have been tested using Postman, including authentication, product management, cart and checkout, orders, wishlist, reviews, product images, and notifications.

## Future Improvements

- Automated unit and integration tests
- Online payment gateway integration
- Automated order notifications
- Product image upload and storage integration
- Docker deployment and CI/CD

## Author

**MD. Abir Ahmed**  
B.Sc. in Computer Science and Engineering
